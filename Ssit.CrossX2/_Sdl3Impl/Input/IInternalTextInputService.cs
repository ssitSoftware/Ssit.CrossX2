using SDL;

namespace Ssit.CrossX2._Sdl3Impl.Input;

internal interface IInternalTextInputService
{
    bool ProcessEvent(SDL_Event @event);
}