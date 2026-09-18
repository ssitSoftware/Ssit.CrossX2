using SDL;

namespace Ssit.CrossX2._Sdl3Impl.Services;

internal unsafe interface IInternalWindowProvider
{
    SDL_Window* Window { get; }
}