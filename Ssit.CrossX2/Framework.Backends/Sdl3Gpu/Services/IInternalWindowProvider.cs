using SDL;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Services;

internal unsafe interface IInternalWindowProvider
{
    SDL_Window* Window { get; }
}