using SDL;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Input;

internal interface IInternalTextInputService
{
    bool ProcessEvent(SDL_Event @event);
}