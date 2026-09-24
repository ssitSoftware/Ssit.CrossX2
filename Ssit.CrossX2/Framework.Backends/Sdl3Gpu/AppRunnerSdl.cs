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
            .WithImplementation<IRenderTarget, SdlGpuRenderTarget>()
            .WithImplementation<IVertexBuffer, SdlGpuVertexBuffer>()
            .WithImplementation<ISoundEffect, SdlSoundEffectImpl>()
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
#endif
        
        SDL_Window* window = SDL_CreateWindow("SDL# GPU Samples"u8, size.Width, size.Height, flags);
        
        if (window == null)
        {
            Console.Error.WriteLine($"SDL_CreateWindow failed: {SDL_GetError()}");
            return;
        }
        
        SDL_GPUDevice* device = SDL_CreateGPUDevice(SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL, true, (byte*)null);
        
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

        var handles = new SdlHandles(window, device);
        var pointingDevices = new SdlPointingDevices(new SdlHandle<SDL_Window>(window));
        
        var appWindowManager = new AppWindowManager(window);

        builder
            .WithSingleton<IRenderer, SdlGpuRenderer>().As<SdlGpuRenderer>()
            .WithSingleton<ISdlGpuPipelineManager, SdlGpuPipelineManager>()
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

        while (appWindowManager.ShouldContinue)
        {
            SDL_Event @event;
            while (SDL_PollEvent(&@event))
            {
                if (true == internalTextInputService?.ProcessEvent(@event))
                {
                    continue;
                }
                
                switch ((SDL_EventType)@event.type)
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

                    case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
                    case SDL_EventType.SDL_EVENT_WINDOW_RESTORED:
                    case SDL_EventType.SDL_EVENT_WINDOW_ENTER_FULLSCREEN:
                    case SDL_EventType.SDL_EVENT_WINDOW_LEAVE_FULLSCREEN:
                    {
                        appWindowManager.EnsureWindowSize();

                        int w, h;
                        SDL_GetWindowSizeInPixels(window, &w, &h);
                        
                        renderHost.Resize(new Size(w, h));
                        component.Resize();
                        
                        SDL_SetWindowMouseGrab(window, false);
                        SDL_SetWindowMouseGrab(window, pointingDevices.LockMouseInWindow);
                        
                        if ((pointingDevices.Mode & PointingDevicesMode.Mouse) == 0)
                        {
                            SDL_HideCursor();
                        }
                        break;
                    }
                    
                    case SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE:
                        SDL_ShowCursor();
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
            
            Render(component,  renderHost, sdlRenderer);

            eventSource.OnRenderFinished();
        }
        
        component.Dispose();
        hostParameters.PostRenderer?.Dispose();
        services.Dispose();
        
        actionScheduler.Process();
        
        SDL_ReleaseWindowFromGPUDevice(device, window);
        SDL_DestroyGPUDevice(device);
        SDL_DestroyWindow(window);
        SDL_Quit();
    }

    private static unsafe void Render(IAppComponent component, RenderHost renderHost, SdlGpuRenderer sdlGpuRenderer)
    {
        sdlGpuRenderer.SubmitCommandBuffer();
        sdlGpuRenderer.StateManager.Reset();
        
        var window = sdlGpuRenderer.Window;
        
        SDL_GPUCommandBuffer* commandBuffer = sdlGpuRenderer.CommandBuffer;
        
        SDL_GPUTexture* swapchainTexture;
        uint swapchainWidth, swapchainHeight;
        
        if (!SDL_WaitAndAcquireGPUSwapchainTexture(commandBuffer, window, &swapchainTexture, &swapchainWidth, &swapchainHeight))
        {
            Console.Error.WriteLine($"SDL_WaitAndAcquireGPUSwapchainTexture failed: {SDL_GetError()}");
            sdlGpuRenderer.SubmitCommandBuffer();
            return;
        }

        if (swapchainTexture != null)
        {
            sdlGpuRenderer.DefaultOutputTarget = new SdlGpuRenderTargetStruct(swapchainTexture, new Size((int)swapchainWidth, (int)swapchainHeight));
            
            sdlGpuRenderer.CurrentPass = RenderPass.Normal;
            if (renderHost.Begin())
            {
                component.Resize();
            }
            
            component.Draw();

            if (renderHost.BeginGlowPass())
            {
                sdlGpuRenderer.CurrentPass = RenderPass.Glow;
                component.Draw();
                sdlGpuRenderer.EndCurrentGpuRenderPass();
                sdlGpuRenderer.SubmitCommandBuffer();
                
                sdlGpuRenderer.CurrentPass = RenderPass.Normal;
            }
            
            renderHost.End();
        }

        sdlGpuRenderer.SubmitCommandBuffer();

        //DebugScreenshot.MaybeCapture(sdlGpuRenderer.Device, window, swapchainTexture, swapchainWidth, swapchainHeight);
    }
}