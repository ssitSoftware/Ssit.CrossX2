using SDL;
using Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Services;
using Ssit.CrossX2.Framework.Input;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Input;

internal unsafe class SdlNativeTextInputService(IInternalWindowProvider windowProvider): INativeTextInputService, IInternalTextInputService
{
    private class NativeTextInput : INativeTextInput
    {
        public event Action Disposed;
        public event Action<RectangleF, int> PositionUpdated;

        public bool IsShiftPressed
        {
            get
            {
                var mod = SDL_GetModState();
                return (mod & (SDL_Keymod.SDL_KMOD_LSHIFT | SDL_Keymod.SDL_KMOD_RSHIFT)) != 0;
            }
        }

        public void Dispose() => Disposed?.Invoke();
        public void UpdatePosition(RectangleF bounds, int cursorPosition)
        {
            PositionUpdated?.Invoke(bounds, cursorPosition);
        }

        public void Reactivate() { }
    }

    private NativeTextInput _currentDisposable;
    private INativeTextInputConsumer _currentConsumer;
    
    public INativeTextInput AllocateTextInput(INativeTextInputConsumer consumer, InputType inputType)
    {
        _currentDisposable?.Dispose();
        _currentDisposable = new NativeTextInput();
        _currentDisposable.Disposed += CurrentNativeTextInputOnDisposed;
        _currentDisposable.PositionUpdated += CurrentNativeTextInputOnPositionUpdated;

        InitTextInput(consumer, inputType);
        
        return _currentDisposable;
    }

    private void CurrentNativeTextInputOnPositionUpdated(RectangleF rect, int cursorPosition)
    {
        
    }

    private void InitTextInput(INativeTextInputConsumer consumer, InputType inputType)
    {
        _currentConsumer = consumer;
        
        SDL_SetTextInputArea(windowProvider.Window, null, 0);
        SDL_StartTextInput(windowProvider.Window);
    }

    private void CurrentNativeTextInputOnDisposed()
    {
        SDL_StopTextInput(windowProvider.Window);
        
        _currentConsumer?.OnTextInputClosed();

        if (_currentDisposable != null)
        {
            _currentDisposable.Disposed -= CurrentNativeTextInputOnDisposed;
            _currentDisposable.PositionUpdated -= CurrentNativeTextInputOnPositionUpdated;
        }

        _currentDisposable = null;
        _currentConsumer = null;
    }

    public bool ProcessEvent(SDL_Event e)
    {
        if (_currentConsumer == null || _currentDisposable == null)
            return false;
        
        switch ((SDL_EventType)e.type)
        {
            case SDL_EventType.SDL_EVENT_KEY_DOWN:

                var key = (Key)e.key.scancode;
                if (_currentConsumer.OnKey(key))
                {
                    return true;
                }
                
                break;
            case SDL_EventType.SDL_EVENT_TEXT_INPUT:
                _currentConsumer.OnTextInput(e.text.GetText());
                return true;
        }

        return false;
    }
}