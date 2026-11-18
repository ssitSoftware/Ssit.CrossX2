using System.Numerics;
using System.Text;
using SDL;
using Ssit.CrossX2.Framework.Audio;
using Ssit.CrossX2.Framework.Audio.Internal;
using Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Audio;
using Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics;
using Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics.Effects;
using Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Input;
using Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Services;
using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Content.Internal;
using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Effects;
using Ssit.CrossX2.Framework.Graphics.Font;
using Ssit.CrossX2.Framework.Graphics.Internal;
using Ssit.CrossX2.Framework.Input;
using Ssit.CrossX2.Framework.Input.Internal;
using Ssit.CrossX2.Framework.IoC.Impl;
using Ssit.CrossX2.Framework.Services;
using Ssit.CrossX2.Framework.Services.Internal;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu;

internal static class AppRunnerSdl
{
    public static void Run(IAppInitializer appInitializer, InitializeServicesDelegate registerServices, InitializeAppDelegate configure = null)
    {
        RunInternal(appInitializer, registerServices, configure);
    }

    private static unsafe void RunInternal(IAppInitializer appInitializer, InitializeServicesDelegate initializeServicesDelegate, InitializeAppDelegate initializeAppDelegate)
    {
        SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_GAMEPAD | SDL_InitFlags.SDL_INIT_AUDIO | SDL_InitFlags.SDL_INIT_HAPTIC);

        var builder = new IoCContainerBuilder();
        var keyboard = new SdlKeyboard();
        var gameControllers = new SdlGameControllers();
        var eventSource = new EventSource();
        var hostParameters = new RenderHostParameters();

        builder
            .WithInstance<IEventSource>(eventSource)
            .WithInstance<IKeyboard>(keyboard)
            .WithInstance<IGameControllers>(gameControllers)
            .WithInstance<IRenderHostParameters>(hostParameters)
            .WithSingleton<ISoundManager, SdlSoundManagerImpl>().As<SdlSoundManagerImpl>()
            .WithSingleton<SdlTrackPool, SdlTrackPool>()
            .WithSingleton<IActionScheduler, ActionScheduler>().As<IInternalActionScheduler>()
            .WithSingleton<IHapticDevice, SdlHapticDevice>()
            .WithSingleton<IFontsManager, FontsManager>()
            .WithSingleton<IContentManager, ContentManager>()
            .WithSingleton<IInputMappings, InputMappings>()
            .WithSingleton<IVirtualGameInput, VirtualGameInput>()
            .WithSingleton<ISmartTextRenderer, SmartTextRenderer>()
            .WithSingleton<IAppTimer, AppTimer>()
            .WithImplementation<ITexture, SdlGpuTexture>()
            .WithImplementation<ICrtSimEffect, CrtSimEffect>()
            .WithImplementation<IGlowEffect, GlowEffect>()
            .WithImplementation<ISharpScaleEffect, SharpScaleEffect>()
            .WithImplementation<IRenderTarget, SdlGpuRenderTarget>()
            .WithImplementation<IVertexBuffer, SdlGpuVertexBuffer>()
            .WithImplementation<ISoundEffect, SdlSoundEffectImpl>()
            .WithImplementation<IContentCache, ContentCache>()
            .WithSingleton<IRenderHost, RenderHost>().As<RenderHost>()
            .WithImplementation<ISingleMusicPlayer, SdlSingleMusicPlayer>();

        initializeServicesDelegate?.Invoke(builder);

        SDL_WindowFlags flags = SDL_WindowFlags.SDL_WINDOW_RESIZABLE;

        var size = new Size(800, 480);
        
        if (appInitializer.ShouldInitializePortraitApp)
        {
            SDL_SetHint(SDL_HINT_ORIENTATIONS, "Portrait");
            size = new Size(480, 800);
        }
        
#if IOS
        SDL_SetHint( SDL_HINT_IOS_HIDE_HOME_INDICATOR, "2" );
        flags = SDL_WindowFlags.SDL_WINDOW_BORDERLESS | SDL_WindowFlags.SDL_WINDOW_FULLSCREEN |
                SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY | SDL_WindowFlags.SDL_WINDOW_METAL;
#elif ANDROID
        flags = SDL_WindowFlags.SDL_WINDOW_BORDERLESS | SDL_WindowFlags.SDL_WINDOW_FULLSCREEN |
                SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY;
#else
        // Without this, macOS defaults to "auto" and hides the menu/title bar
        // (and its green button) because fullscreen was toggled programmatically
        // rather than via the window's own fullscreen button.
        SDL_SetHint(SDL_HINT_VIDEO_MAC_FULLSCREEN_MENU_VISIBILITY, "1");
#endif
        
        SDL_Window* window = SDL_CreateWindow(appInitializer.WindowTitle, size.Width, size.Height, flags);
        
        if (window == null)
        {
            Console.Error.WriteLine($"SDL_CreateWindow failed: {SDL_GetError()}");
            return;
        }
        
        // GPU debug mode (validation layers) costs a lot of frame time, so it is only used by Debug builds.
#if DEBUG
        const bool gpuDebugMode = true;
#else
        const bool gpuDebugMode = false;
#endif
        SDL_GPUDevice* device = CreateGpuDevice(gpuDebugMode);

        if (device == null && gpuDebugMode)
        {
            // Debug layers (e.g. the D3D12 debug layer from Windows "Graphics Tools") may be missing.
            Console.Error.WriteLine($"SDL_CreateGPUDevice with debug mode failed: {SDL_GetError()}. Retrying without debug mode.");
            device = CreateGpuDevice(false);
        }

        if (device == null)
        {
            Console.Error.WriteLine($"SDL_CreateGPUDevice failed: {SDL_GetError()}");
            return;
        }

        if (!SDL_ClaimWindowForGPUDevice(device, window))
        {
            Console.Error.WriteLine($"SDL_ClaimWindowForGPUDevice failed: {SDL_GetError()}");
            return;
        }

        var deviceProperties = SDL_GetGPUDeviceProperties(device);
        Console.WriteLine($"[GPU] {SDL_GetGPUDeviceDriver(device)}: {SDL_GetStringProperty(deviceProperties, SDL_PROP_GPU_DEVICE_NAME_STRING, "unknown")} " +
                          $"(driver {SDL_GetStringProperty(deviceProperties, SDL_PROP_GPU_DEVICE_DRIVER_VERSION_STRING, "?")}, debug mode {gpuDebugMode})");

        var perfStats = FramePerfStats.CreateIfEnabled();

        var handles = new SdlHandles(window, device);
        var pointingDevices = new SdlPointingDevices(new SdlHandle<SDL_Window>(window));

        var appWindowManager = new AppWindowManager(window, device);

        // CROSSX_PRESENT_MODE pins the present mode; IAppWindowManager.VSync is then ignored.
        if (GetRequestedPresentMode() is { } forcedMode)
        {
            appWindowManager.ForcePresentMode(forcedMode);
        }

        builder
            .WithSingleton<IRenderer, SdlGpuRenderer>().As<SdlGpuRenderer>()
            .WithSingleton<ISdlGpuPipelineManager, SdlGpuPipelineManager>()
            .WithSingleton<ISnapshotService, SdlGpuSnapshotService>()
            .WithInstance<IAppWindowManager>(appWindowManager).As<IInternalWindowProvider>()
            .WithInstance<IPointingDevices>(pointingDevices).As<IInputHandler>()
            .WithInstance(handles)
            .WithPostBuildDelegate<IActionScheduler>(scheduler => appWindowManager.Initialize(scheduler));

        appInitializer.RegisterServices(builder);
        var services = builder.Build();

        var actionScheduler = services.Get<IInternalActionScheduler>();
        var updatables = services.Fetch<IUpdatable>().ToArray();
        var sdlRenderer = services.Get<SdlGpuRenderer>();
        
        var component = appInitializer.Initialize(services, hostParameters);

        var renderHost = services.Get<RenderHost>();
        
        component.Initialize();
        component.SetActive(true);
        
        actionScheduler.Process();
        
        if ((pointingDevices.Mode & PointingDevicesMode.Mouse) == 0)
        {
            SDL_HideCursor();
        }
        
        initializeAppDelegate?.Invoke(services);
        
        var lastTicks = SDL_GetTicksNS();
        bool shouldDisplayAndUpdate = true;

        eventSource.Paused += () => shouldDisplayAndUpdate = false;
        eventSource.Resumed += () =>
        {
            shouldDisplayAndUpdate = true;
            lastTicks = SDL_GetTicksNS();
        };
        
#if IOS
        AppEventWatcher.AppActivated += eventSource.OnResume;
        AppEventWatcher.AppDeativated += eventSource.OnPause;
#endif

        services.TryGet<IInternalTextInputService>(out var internalTextInputService);

        Size previousWindowSize = Size.Zero;

        // The first frame after start is rendered before the render host has created its
        // render targets (Begin() only schedules the initial Resize), so drawing it shows
        // uninitialized (magenta) output. Present it as plain black instead.
        bool isFirstFrame = true;
        
        while (appWindowManager.ShouldContinue)
        {
            SDL_Event @event;
            while (SDL_PollEvent(&@event))
            {
                if (true == internalTextInputService?.ProcessEvent(@event))
                {
                    continue;
                }
                
                var eventType = (SDL_EventType)@event.type;
                switch (eventType)
                {
                    case SDL_EventType.SDL_EVENT_QUIT:
                    {
                        var exitArgs = new WindowClosingEventArgs();
                        appWindowManager.RaiseAppExiting(exitArgs);

                        if (!exitArgs.Cancel)
                        {
                            Console.WriteLine("[AppRunner] SDL_EVENT_QUIT received — closing app");
                            appWindowManager.Close();
                            shouldDisplayAndUpdate = false;
                        }
                    }
                    break;

#if !ANDROID && !IOS
                    case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:
                        eventSource.OnPause();
                        break;
                    
                    case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED:
                        eventSource.OnResume();
                        component.SetActive(true);
                        break;
#endif
                    
                    case SDL_EventType.SDL_EVENT_DID_ENTER_BACKGROUND:
                        shouldDisplayAndUpdate = false;
                        eventSource.OnPause();
                        component.SetActive(false);
                        break;
                    
                    case SDL_EventType.SDL_EVENT_WILL_ENTER_BACKGROUND:
                        shouldDisplayAndUpdate = false;
                        eventSource.OnPause();
                        component.SetActive(false);
                        break;
                    
                    case SDL_EventType.SDL_EVENT_TERMINATING:
                        Console.WriteLine("[AppRunner] SDL_EVENT_TERMINATING received — closing app");
                        shouldDisplayAndUpdate = false;
                        appWindowManager.Close();
                        break;
                    
                    case SDL_EventType.SDL_EVENT_DID_ENTER_FOREGROUND:
                        shouldDisplayAndUpdate = true;
                        eventSource.OnResume();
                        component.SetActive(true);
                        break;

                    case SDL_EventType.SDL_EVENT_WINDOW_ENTER_FULLSCREEN:
                    case SDL_EventType.SDL_EVENT_WINDOW_LEAVE_FULLSCREEN:
                    {
                        // SDL only raises these events once it considers the transition
                        // finalized, so there's no need to force a sync here. Doing so anyway,
                        // or rebuilding swapchain-sized resources (e.g. the glow effect)
                        // synchronously on this path, risks landing a window-state call while
                        // a macOS Space animation is still settling - which can leave SDL's
                        // internal "pending operation" flag stuck and make every later
                        // SDL_SyncWindow call block for its full ~2.5s Cocoa timeout. Defer the
                        // actual resize/rebuild to the next frame instead.
                        int w, h;
                        SDL_GetWindowSizeInPixels(window, &w, &h);
                        var newSize = new Size(w, h);

                        if (eventType == SDL_EventType.SDL_EVENT_WINDOW_LEAVE_FULLSCREEN)
                        {
                            appWindowManager.SetWindowed();
                        }

                        appWindowManager.UpdateSize(newSize);

                        actionScheduler.Schedule(() =>
                        {
                            renderHost.Resize(newSize);
                            component.Resize();
                        });

                        SDL_SetWindowMouseGrab(window, false);
                        SDL_SetWindowMouseGrab(window, pointingDevices.LockMouseInWindow);

                        if ((pointingDevices.Mode & PointingDevicesMode.Mouse) == 0)
                        {
                            SDL_HideCursor();
                        }

                        actionScheduler.Schedule(() =>
                        {
                            eventSource.OnPause();
                            eventSource.OnResume();
                        });

                        if (eventType == SDL_EventType.SDL_EVENT_WINDOW_ENTER_FULLSCREEN)
                        {
                            eventSource.OnEnteredFullscreen();
                        }
                        else
                        {
                            eventSource.OnLeftFullscreen();
                        }
                        break;
                    }

                    case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
                    case SDL_EventType.SDL_EVENT_WINDOW_RESTORED:
                    {
                        // No SDL_SyncWindow here: macOS also sends RESIZED mid fullscreen
                        // transition, and syncing then can trip SDL's stuck pending-operation
                        // state (each later sync blocks ~2.5s). Defer the rebuild instead.
                        int w, h;
                        SDL_GetWindowSizeInPixels(window, &w, &h);
                        var newSize = new Size(w, h);

                        appWindowManager.UpdateSize(newSize);

                        actionScheduler.Schedule(() =>
                        {
                            renderHost.Resize(newSize);
                            component.Resize();
                        });

                        SDL_SetWindowMouseGrab(window, false);
                        SDL_SetWindowMouseGrab(window, pointingDevices.LockMouseInWindow);

                        if ((pointingDevices.Mode & PointingDevicesMode.Mouse) == 0)
                        {
                            SDL_HideCursor();
                        }

                        actionScheduler.Schedule(() =>
                        {
                            eventSource.OnPause();
                            eventSource.OnResume();
                        });
                        break;
                    }

                    case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
                        break;
                    
                    case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER:
                        if ((pointingDevices.Mode & PointingDevicesMode.Mouse) == 0)
                        {
                            SDL_HideCursor();
                        }
                        break;
                    
                    case SDL_EventType.SDL_EVENT_KEY_DOWN:
                        (component as IKeyboardEventHandler)?.OnKeyDown((Key)@event.key.scancode);
                        break;
                    
                    case SDL_EventType.SDL_EVENT_KEY_UP:
                        (component as IKeyboardEventHandler)?.OnKeyUp((Key)@event.key.scancode);
                        break;
                }
                
                gameControllers.ProcessEvent(@event);
            }

            // When nothing else drives the cursor (no Mouse / AutoHideMouse mode), keep it hidden.
            // SDL's hidden cursor only applies over our window, so the system cursor outside the
            // window is unaffected. Re-checked every frame because macOS can drop the matching
            // MOUSE_ENTER (fullscreen transitions, menu bar reveal, Space switches).
            if ((pointingDevices.Mode & (PointingDevicesMode.Mouse | PointingDevicesMode.AutoHideMouse)) == 0 &&
                SDL_CursorVisible())
            {
                SDL_HideCursor();
            }
            
            actionScheduler.Process();
            
            if (!shouldDisplayAndUpdate) continue;
            
            var ticks = SDL_GetTicksNS();
            var dt = (ticks - lastTicks) / 1000000000.0;
            lastTicks = ticks;
            
            pointingDevices.Update();
            keyboard.Update();
            
            eventSource.OnUpdate((float)dt);
            
            foreach(var updatable in updatables)
            {
                updatable.Update((float)dt);
            }
            
            component.Update((float)dt);
            eventSource.OnUpdated();
            gameControllers.PostUpdate();

            var updateEndTicks = SDL_GetTicksNS();

            int ww, wh;
            SDL_GetWindowSizeInPixels(window, &ww, &wh);

            if (previousWindowSize.Width != ww || previousWindowSize.Height != wh)
            {
                previousWindowSize = new Size(ww, wh);
                continue;
            }

            try
            {
                if (Render(component, renderHost, sdlRenderer, skipDraw: isFirstFrame))
                {
                    isFirstFrame = false;
                }
            }
            catch
            {
                renderHost.Resize(previousWindowSize);
                component.Resize();
            }

            eventSource.OnRenderFinished();
            perfStats?.OnFrame(renderHost, updateNs: updateEndTicks - ticks,
                renderNs: SDL_GetTicksNS() - updateEndTicks, presentNs: sdlRenderer.LastPresentNs);
        }
        
        component.Dispose();
        actionScheduler.Process();
        
        hostParameters.PostRenderer?.Dispose();
        actionScheduler.Process();
        
        services.Dispose();
        actionScheduler.Process();

        WriteMemoryLeakReport();

        sdlRenderer.ReleaseBackBuffer();
        SDL_ReleaseWindowFromGPUDevice(device, window);
        SDL_DestroyGPUDevice(device);
        SDL_DestroyWindow(window);
        SDL_Quit();
    }

    private static void WriteMemoryLeakReport()
    {
        if (SdlGpuTexture.Count > 0)
        {
            Console.WriteLine($"Leaked textures: {SdlGpuTexture.Count}");
        }

        if (SdlGpuRenderTarget.Count > 0)
        {
            Console.WriteLine($"Leaked render targets: {SdlGpuRenderTarget.Count}");
        }

        if (SdlSoundEffectImpl.Count > 0)
        {
            Console.WriteLine($"Leaked sound effects: {SdlSoundEffectImpl.Count}");
        }
    }

    /// <returns>True when a swapchain frame was presented.</returns>
    private static unsafe bool Render(IAppComponent component, RenderHost renderHost, SdlGpuRenderer sdlGpuRenderer, bool skipDraw = false)
    {
        sdlGpuRenderer.SubmitCommandBuffer();
        sdlGpuRenderer.StateManager.Reset();

        var window = sdlGpuRenderer.Window;

        // The frame is drawn into an offscreen back buffer and only copied to the swapchain in Present().
        // The swapchain texture is presented when the command buffer that acquired it is submitted, and
        // the frame submits mid-way (glow/CRT effects, render queue overflow, snapshots), so acquiring it
        // up front would present early and keep drawing into a retired back buffer, which D3D12 rejects.
        int windowWidth, windowHeight;
        SDL_GetWindowSizeInPixels(window, &windowWidth, &windowHeight);

        if (!sdlGpuRenderer.PrepareBackBuffer(new Size(windowWidth, windowHeight)))
        {
            return false;
        }

        try
        {
            sdlGpuRenderer.CurrentPass = RenderPass.Normal;
            if (renderHost.Begin())
            {
                component.Resize();
            }

            if (skipDraw)
            {
                // Present a black frame; the scheduled Resize runs before the next one.
                sdlGpuRenderer.SetRenderTarget(null);
                sdlGpuRenderer.Clear(RgbaColor.Black);
                return sdlGpuRenderer.Present();
            }

            component.Draw();

            if (renderHost.BeginGlowPass())
            {
                sdlGpuRenderer.CurrentPass = RenderPass.Glow;
                component.Draw();
                sdlGpuRenderer.EndCurrentGpuRenderPass();

                sdlGpuRenderer.CurrentPass = RenderPass.Normal;
            }

            renderHost.End();
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: " + ex.Message);

            try
            {
                sdlGpuRenderer.SetRenderTarget(null);
                sdlGpuRenderer.Clear(RgbaColor.Black);
                sdlGpuRenderer.Present();
            }
            catch (Exception presentEx)
            {
                Console.WriteLine("ERROR: " + presentEx.Message);
            }

            throw;
        }

        return sdlGpuRenderer.Present();

        //DebugScreenshot.MaybeCapture(sdlGpuRenderer.Device, window, swapchainTexture, swapchainWidth, swapchainHeight);
    }

    /// <summary>
    /// Creates the GPU device for the shader formats this platform ships shaders for. SDL picks the GPU driver from these:
    /// Metal (MSL) on Apple platforms, Direct3D 12 (DXBC compiled at runtime from HLSL) on Windows.
    /// Set the environment variable CROSSX_GPU_PREFER_LOW_POWER=1 to pick the integrated GPU on dual-GPU machines.
    /// </summary>
    private static unsafe SDL_GPUDevice* CreateGpuDevice(bool debugMode)
    {
        var props = SDL_CreateProperties();

        try
        {
            if (OperatingSystem.IsWindows())
            {
                SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_SHADERS_DXBC_BOOLEAN, true);
            }
            else
            {
                SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_SHADERS_MSL_BOOLEAN, true);
            }

            SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_DEBUGMODE_BOOLEAN, debugMode);
            SDL_SetBooleanProperty(props, SDL_PROP_GPU_DEVICE_CREATE_PREFERLOWPOWER_BOOLEAN,
                Environment.GetEnvironmentVariable("CROSSX_GPU_PREFER_LOW_POWER") == "1");

            return SDL_CreateGPUDeviceWithProperties(props);
        }
        finally
        {
            SDL_DestroyProperties(props);
        }
    }

    /// <summary>
    /// Present mode requested with the environment variable CROSSX_PRESENT_MODE (vsync, mailbox or immediate);
    /// null when not set, which keeps SDL's default.
    /// </summary>
    private static SDL_GPUPresentMode? GetRequestedPresentMode() =>
        Environment.GetEnvironmentVariable("CROSSX_PRESENT_MODE")?.Trim().ToLowerInvariant() switch
        {
            "vsync" => SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_VSYNC,
            "mailbox" => SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_MAILBOX,
            "immediate" => SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_IMMEDIATE,
            _ => null
        };

    /// <summary>
    /// Frame time statistics, enabled with the environment variable CROSSX_PERF=1.
    /// Present mode is left alone: turn VSync off (or set CROSSX_PRESENT_MODE) to measure uncapped frame times.
    /// </summary>
    private sealed class FramePerfStats
    {
        private ulong _windowStartTicks;
        private ulong _lastTicks;
        private int _frames;
        private double _worstMs;
        private ulong _updateNs;
        private ulong _renderNs;
        private ulong _presentNs;
        private int _gcCount;
        private long _allocatedBytes;

        public static FramePerfStats CreateIfEnabled() =>
            Environment.GetEnvironmentVariable("CROSSX_PERF") == "1" ? new FramePerfStats() : null;

        /// <param name="updateNs">Input + app update.</param>
        /// <param name="renderNs">Whole Render call, including <paramref name="presentNs"/>.</param>
        /// <param name="presentNs">Swapchain wait + final submit, which is mostly waiting for the GPU.</param>
        public void OnFrame(RenderHost renderHost, ulong updateNs, ulong renderNs, ulong presentNs)
        {
            var now = SDL_GetTicksNS();

            if (_windowStartTicks == 0)
            {
                _windowStartTicks = now;
                _lastTicks = now;
                _gcCount = GC.CollectionCount(0);
                _allocatedBytes = GC.GetAllocatedBytesForCurrentThread();
                return;
            }

            _worstMs = Math.Max(_worstMs, (now - _lastTicks) / 1e6);
            _lastTicks = now;
            _frames++;
            _updateNs += updateNs;
            _renderNs += renderNs;
            _presentNs += Math.Min(presentNs, renderNs);

            var elapsed = (now - _windowStartTicks) / 1e9;

            if (elapsed < 2.0)
                return;

            var gcCount = GC.CollectionCount(0);
            var allocatedBytes = GC.GetAllocatedBytesForCurrentThread();
            double PerFrameMs(ulong ns) => ns / 1e6 / _frames;

            Console.WriteLine($"[Perf] {_frames / elapsed:F1} fps, avg {elapsed * 1000 / _frames:F2} ms, worst {_worstMs:F2} ms, " +
                              $"update {PerFrameMs(_updateNs):F2} ms, draw {PerFrameMs(_renderNs - _presentNs):F2} ms, " +
                              $"present/GPU wait {PerFrameMs(_presentNs):F2} ms, " +
                              $"batches {Graphics.Renderers.SdlGpuRenderQueue.FlushCount / (double)_frames:F0}/frame, render passes {SdlGpuRenderer.RenderPassCount / (double)_frames:F0}/frame, " +
                              $"alloc {(allocatedBytes - _allocatedBytes) / 1024.0 / _frames:F1} KB/frame, gen0 GCs {gcCount - _gcCount}, " +
                              $"scene {renderHost.TargetSize.Width}x{renderHost.TargetSize.Height}");
            _windowStartTicks = now;
            _frames = 0;
            _worstMs = 0;
            _updateNs = _renderNs = _presentNs = 0;
            Graphics.Renderers.SdlGpuRenderQueue.FlushCount = 0;
            SdlGpuRenderer.RenderPassCount = 0;
            _gcCount = gcCount;
            _allocatedBytes = allocatedBytes;
        }
    }
}
