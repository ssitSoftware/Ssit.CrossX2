using SDL;
using Ssit.CrossX2.Framework.Services;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Services;

internal unsafe class AppWindowManager(SDL_Window* window, SDL_GPUDevice* device): IAppWindowManager, IInternalWindowProvider
{
    private IActionScheduler _actionScheduler;
    private SDL_GPUPresentMode? _forcedPresentMode;

    public bool VSync
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;

            if (_forcedPresentMode != null)
                return;

            var mode = value ? SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_VSYNC : SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_IMMEDIATE;

            // Applied between frames: the swapchain must not be changed while a swapchain texture is acquired.
            if (_actionScheduler != null)
            {
                _actionScheduler.Schedule(() => ApplyPresentMode(mode));
            }
            else
            {
                ApplyPresentMode(mode);
            }
        }
    } = true; // SDL's default present mode is VSYNC.

    /// <summary>
    /// Pins the present mode (CROSSX_PRESENT_MODE / perf stats); later <see cref="VSync"/> changes are ignored.
    /// </summary>
    public void ForcePresentMode(SDL_GPUPresentMode mode)
    {
        _forcedPresentMode = mode;
        ApplyPresentMode(mode);
    }

    private void ApplyPresentMode(SDL_GPUPresentMode requestedMode)
    {
        // IMMEDIATE and MAILBOX are optional in SDL; fall back to the other uncapped mode, then to VSYNC (always supported).
        SDL_GPUPresentMode[] candidates = requestedMode == SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_VSYNC
            ? [requestedMode]
            : [requestedMode, requestedMode == SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_MAILBOX
                ? SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_IMMEDIATE
                : SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_MAILBOX];

        var mode = candidates.FirstOrDefault(m => SDL_WindowSupportsGPUPresentMode(device, window, m), SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_VSYNC);

        if (!SDL_SetGPUSwapchainParameters(device, window, SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR, mode))
        {
            Console.Error.WriteLine($"SDL_SetGPUSwapchainParameters({mode}) failed: {SDL_GetError()}");
        }
        else
        {
            Console.WriteLine($"[GPU] present mode {mode}");
        }
    }
    public bool ShouldContinue { get; private set; } = true;
    private Size _minimumSize = new(160, 90);
    private Size _windowSize = new(320, 180);
    private Size _lastWindowSize = new(320, 180);
    private WindowedMode _windowedMode = WindowedMode.KeepAspect;

    private bool _firstTimeWindowed = true;
    
    public event Action<WindowClosingEventArgs> Closing;

    public void Initialize(IActionScheduler actionScheduler)
    {
        _actionScheduler = actionScheduler;
    }
    
    public void Close()
    {
        ShouldContinue = false;
    }

    public bool IsFullscreen => (SDL_GetWindowFlags(window) & SDL_WindowFlags.SDL_WINDOW_FULLSCREEN) != 0;

    public bool SetFullscreen()
    {
        _actionScheduler.Schedule(() =>
            {
                var flags = SDL_GetWindowFlags(window);
                if ((flags & SDL_WindowFlags.SDL_WINDOW_FULLSCREEN) == 0)
                {
                    if (_firstTimeWindowed)
                    {
                        SDL_SetWindowSize(window, _lastWindowSize.Width, _lastWindowSize.Height);
                        SDL_SetWindowPosition(window, (int)SDL_WINDOWPOS_CENTERED, (int)SDL_WINDOWPOS_CENTERED);
                        _firstTimeWindowed = false;
                    }
                    ApplyFullscreenMenuVisibility();
                    SDL_SetWindowFullscreen(window, true);
                }
            }
        );
        return true;
    }

    public bool SetWindowed()
    {
        _actionScheduler.Schedule(() =>
        {
            var flags = SDL_GetWindowFlags(window);
            if ((flags & SDL_WindowFlags.SDL_WINDOW_FULLSCREEN) != 0)
            {
                // Don't SDL_SyncWindow here - the OS will notify us once the transition
                // actually finishes via SDL_EVENT_WINDOW_LEAVE_FULLSCREEN, which is handled
                // separately. Force-waiting on it here as well risks colliding with a still-
                // settling macOS Space animation and tripping SDL's "stuck pending operation"
                // bug, where every later SDL_SyncWindow call blocks for its full timeout.
                SDL_SetWindowFullscreen(window, false);
            }

            var size = _lastWindowSize;
            
            switch (_windowedMode &  WindowedMode.SizeModes)
            {
                case WindowedMode.None:
                    SDL_SetWindowMinimumSize(window, _minimumSize.Width, _minimumSize.Height);
                    break;
                
                case WindowedMode.KeepAspect:
                    float aspect = (float)_windowSize.Width / _windowSize.Height;
                    SDL_SetWindowMinimumSize(window, _minimumSize.Width, _minimumSize.Height);
                    SDL_SetWindowAspectRatio(window, aspect, aspect);
                    SDL_SetWindowMaximumSize(window, short.MaxValue, short.MaxValue);
                    break;
                
                case WindowedMode.KeepSize:
                    aspect = (float)_windowSize.Width / _windowSize.Height;
                    size = _windowSize;
                    
                    SDL_SetWindowAspectRatio(window, aspect, aspect);
                    SDL_SetWindowMinimumSize(window, _windowSize.Width, _windowSize.Height);
                    SDL_SetWindowMaximumSize(window, _windowSize.Width, _windowSize.Height);
                    break;
            }

            SDL_SetWindowSize(window, size.Width, size.Height);

            if (_firstTimeWindowed)
            {
                SDL_SetWindowPosition(window, (int)SDL_WINDOWPOS_CENTERED, (int)SDL_WINDOWPOS_CENTERED);
                _firstTimeWindowed = false;
            }
        });

        return true;
    }

    public void SetWindowParameters(Size size, Size minimumSize, WindowedMode mode = WindowedMode.KeepAspect)
    {
        _windowSize = size;
        _lastWindowSize = size;
        _minimumSize = minimumSize;
        _windowedMode = mode;

        // SDL applies the hint to a window that is already fullscreen, so a flag change takes effect immediately.
        if (_actionScheduler != null && IsFullscreen)
        {
            _actionScheduler.Schedule(ApplyFullscreenMenuVisibility);
        }
    }

    /// <summary>
    /// macOS: controls whether the menu bar / title bar can be revealed by moving the cursor to the top edge
    /// of a fullscreen Space. With <see cref="WindowedMode.BorderlessFullscreen"/> they stay hidden ("0"),
    /// otherwise they behave like a regular macOS fullscreen window ("1"). Must be set before
    /// SDL_SetWindowFullscreen - SDL's default ("auto") would hide them for programmatic fullscreen.
    /// </summary>
    private void ApplyFullscreenMenuVisibility()
    {
        if (!OperatingSystem.IsMacOS())
            return;

        var borderless = (_windowedMode & WindowedMode.BorderlessFullscreen) != 0;
        SDL_SetHint(SDL_HINT_VIDEO_MAC_FULLSCREEN_MENU_VISIBILITY, borderless ? "0" : "1");
    }

    public void SetTitle(string title) => SDL_SetWindowTitle(window, title);

    public bool IsTouchScreen
    {
        get
        {
            var platform = SDL_GetPlatform()?.ToLowerInvariant();
            return platform is "android" or "ios";
        }
    }

    public Size GetWindowMaxSize()
    {
        var displayId = SDL_GetDisplayForWindow(window);
        SDL_Rect rect;
        SDL_GetDisplayBounds(displayId, &rect);
        return new Size(rect.w, rect.h);
    }

    public void SetMinimumSize(Size size)
    {
        
        _actionScheduler.Schedule(() =>
        {
            SDL_SetWindowMinimumSize(window, size.Width, size.Height);
        });
    }

    public (int w, int h, int hz) GetDisplayMode()
    {
        var display =  SDL_GetDisplayForWindow(window);
        var mode = SDL_GetCurrentDisplayMode(display);

        return (mode->w, mode->h, (int)Math.Ceiling(mode->refresh_rate));
    }

    public void RaiseAppExiting(WindowClosingEventArgs args)
    {
        Closing?.Invoke(args);
    }

    public SDL_Window* Window => window;

    public void UpdateSize(Size size)
    {
        var flags = SDL_GetWindowFlags(window);
        if ((flags & SDL_WindowFlags.SDL_WINDOW_FULLSCREEN) == 0)
        {
            _lastWindowSize = size;
        }
    }
}