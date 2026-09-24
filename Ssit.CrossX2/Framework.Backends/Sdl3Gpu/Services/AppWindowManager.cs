using SDL;
using Ssit.CrossX2.Framework.Services;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Services;

internal unsafe class AppWindowManager(SDL_Window* window): IAppWindowManager, IInternalWindowProvider
{
    private IActionScheduler _actionScheduler;
    public bool ShouldContinue { get; private set; } = true;
    private Size _minimumSize = new(160, 90);

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
                    SDL_SetWindowFullscreen(window, true);
                    SDL_SyncWindow(window);
                }
            }
        );
        return true;
    }

    public bool SetWindowed(Size size, WindowedMode mode)
    {
        _actionScheduler.Schedule(() =>
        {
            var flags = SDL_GetWindowFlags(window);
            if ((flags & SDL_WindowFlags.SDL_WINDOW_FULLSCREEN) != 0)
            {
                SDL_SetWindowFullscreen(window, false);
                SDL_SyncWindow(window);
            }

            switch (mode)
            {
                case WindowedMode.None:
                    SDL_SetWindowMinimumSize(window, _minimumSize.Width, _minimumSize.Height);
                    break;
                
                case WindowedMode.KeepAspect:
                    float aspect = (float)size.Width / size.Height;
                    SDL_SetWindowMinimumSize(window, _minimumSize.Width, _minimumSize.Height);
                    SDL_SetWindowAspectRatio(window, aspect, aspect);
                    SDL_SetWindowMaximumSize(window, short.MaxValue, short.MaxValue);
                    break;
                
                case WindowedMode.KeepSize:
                    aspect = (float)size.Width / size.Height;
                    SDL_SetWindowAspectRatio(window, aspect, aspect);
                    SDL_SetWindowMinimumSize(window, size.Width, size.Height);
                    SDL_SetWindowMaximumSize(window, size.Width, size.Height);
                    break;
            }

            SDL_SetWindowSize(window, size.Width, size.Height);
            SDL_SetWindowPosition(window, (int)SDL_WINDOWPOS_CENTERED, (int)SDL_WINDOWPOS_CENTERED);
            SDL_SyncWindow(window);
        });

        return true;
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
        _minimumSize = size;
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
}