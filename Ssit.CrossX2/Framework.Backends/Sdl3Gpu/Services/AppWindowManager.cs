using SDL;
using Ssit.CrossX2.Framework.Services;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Services;

internal unsafe class AppWindowManager(SDL_Window* window): IAppWindowManager, IInternalWindowProvider
{
    private IActionScheduler _actionScheduler;
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
                    SDL_SetWindowFullscreen(window, true);
                    SDL_SyncWindow(window);
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
                SDL_SetWindowFullscreen(window, false);
                SDL_SyncWindow(window);
            }

            var size = _lastWindowSize;
            
            switch (_windowedMode)
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

            SDL_SyncWindow(window);
        });

        return true;
    }

    public void SetWindowParameters(Size size, Size minimumSize, WindowedMode mode = WindowedMode.KeepAspect)
    {
        _windowSize = size;
        _lastWindowSize = size;
        _minimumSize = minimumSize;
        _windowedMode = mode;
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