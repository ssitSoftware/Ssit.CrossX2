using SDL;
using Ssit.CrossX2._Sdl3Impl.Audio;
using Ssit.CrossX2._Sdl3Impl.Graphics;
using Ssit.CrossX2._Sdl3Impl.Input;
using Ssit.CrossX2._Sdl3Impl.Services;
using Ssit.CrossX2.Audio;
using Ssit.CrossX2.Audio.Internal;
using Ssit.CrossX2.Core;
using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.Input;
using Ssit.CrossX2.Input.Internal;
using Ssit.CrossX2.IoC.Impl;
using Ssit.CrossX2.Services;
using Ssit.CrossX2.Services.Internal;
using static SDL.SDL3;
using RendererComponents = Ssit.CrossX2.Graphics.RendererComponents;

namespace Ssit.CrossX2._Sdl3Impl;

public static class AppRunnerSdl
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
            // .WithImplementation<ITexture, SdlTexture>()
            .WithImplementation<IRenderTarget, SdlGpuRenderTarget>()
            .WithImplementation<IVertexBuffer, SdlGpuVertexBuffer>()
            .WithSingleton<ISoundManager, SdlSoundManagerImpl>().As<SdlSoundManagerImpl>()
            .WithSingleton<SdlTrackPool, SdlTrackPool>()
            .WithImplementation<ISoundEffect, SdlSoundEffectImpl>()
            .WithImplementation<ISingleMusicPlayer, SdlSingleMusicPlayer>()
            .WithSingleton<IHapticDevice, SdlHapticDevice>();
            //.WithPixelCore();

        initializeServicesDelegate?.Invoke(builder);

        SDL_WindowFlags flags = 0;

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
        var sdlRenderer = new SdlGpuRenderer(device, window);
        
        builder
            .WithInstance<IRenderer>(sdlRenderer)
            .WithSingleton<ISdlGpuPipelineManager, SdlGpuPipelineManager>()
            .WithInstance<IAppWindowManager>(appWindowManager).As<IInternalWindowProvider>()
            .WithInstance<IPointingDevices>(pointingDevices).As<IInputHandler>()
            .WithInstance(handles);

        appInitializer.RegisterServices(builder);
        appInitializer.InitializeRenderHost(hostParameters);
        
        var services = builder.Build();

        var actionScheduler = services.Get<IActionScheduler>();

        var updatables = services.Fetch<IUpdatable>().ToArray();
        
        appWindowManager.Initialize(actionScheduler);
        
        using IAppComponent component = appInitializer.CreateAppComponent(services);
        component.SetActive(true);
        
        (actionScheduler as IInternalActionScheduler)?.Process();
        
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
                        component.Resize();
                        appWindowManager.EnsureWindowSize();

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

            Render(component, sdlRenderer);
            
            // Present!!
            //SDL_RenderPresent(renderer);
            eventSource.OnRenderFinished();
        }
        
        services.Dispose();
        
        SDL_ReleaseWindowFromGPUDevice(device, window);
        SDL_DestroyGPUDevice(device);
        SDL_DestroyWindow(window);
        SDL_Quit();
    }

    private static unsafe void Render(IAppComponent component, SdlGpuRenderer sdlGpuRenderer)
    {
        var window = sdlGpuRenderer.Window;

        SDL_GPUCommandBuffer* commandBuffer = sdlGpuRenderer.CommandBuffer;
        
        SDL_GPUTexture* swapchainTexture;
        uint swapchainWidth, swapchainHeight;

        if (!SDL_WaitAndAcquireGPUSwapchainTexture(commandBuffer, window, &swapchainTexture, &swapchainWidth, &swapchainHeight))
        {
            Console.Error.WriteLine($"SDL_WaitAndAcquireGPUSwapchainTexture failed: {SDL_GetError()}");
            SDL_SubmitGPUCommandBuffer(commandBuffer);
            return;
        }

        if (swapchainTexture != null)
        {
            sdlGpuRenderer.DefaultOutputTarget = new SdlGpuRenderTargetStruct(swapchainTexture, new Size((int)swapchainWidth, (int)swapchainHeight));
            component.Draw();
        }

        sdlGpuRenderer.SubmitCommandBuffer();
    }
}