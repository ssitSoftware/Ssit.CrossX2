using System.Numerics;
using SDL;
using Ssit.CrossX2.Framework.Input;
using Ssit.CrossX2.Framework.Input.Internal;
using static SDL.SDL3;

#if !IOS && !ANDROID

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Input;

internal partial class SdlPointingDevices
{

    private SdlPointingDevices()
    {
    }

    private void ProcessTouch()
    {
    }

    private unsafe void AnalyzeMouse()
    {
        if ((Mode & PointingDevicesMode.Mouse) != 0)
        {
            float x, y;
            var state = SDL_GetMouseState(&x, &y);
            var focus = SDL_GetMouseFocus();
            var position = new Vector2(x, y);

            var leftButtonState = ButtonState.FromStates(state.HasFlag(SDL_MouseButtonFlags.SDL_BUTTON_LMASK),
                _previousFlags.HasFlag(SDL_MouseButtonFlags.SDL_BUTTON_LMASK));

            var rightButtonState = ButtonState.FromStates(state.HasFlag(SDL_MouseButtonFlags.SDL_BUTTON_RMASK),
                _previousFlags.HasFlag(SDL_MouseButtonFlags.SDL_BUTTON_RMASK));

            var middleButtonState = ButtonState.FromStates(state.HasFlag(SDL_MouseButtonFlags.SDL_BUTTON_MMASK),
                _previousFlags.HasFlag(SDL_MouseButtonFlags.SDL_BUTTON_MMASK));

            var moved = Math.Abs(_previousMousePosition.X - x) > float.Epsilon ||
                        Math.Abs(_previousMousePosition.Y - y) > float.Epsilon;

            ProcessButton(leftButtonState, MouseButtons.Left, moved, position);
            ProcessButton(rightButtonState, MouseButtons.Right, moved, position);
            ProcessButton(middleButtonState, MouseButtons.Middle, moved, position);
            
            _previousFlags = state;
            _previousMousePosition = position;
        
            UpdateHoverPosition(focus is null ? null : _previousMousePosition);
            
            if (_mouseInWindowLocked != LockMouseInWindow)
            {
                SDL_SetWindowMouseGrab(_windowHandle.Pointer, LockMouseInWindow);
                _mouseInWindowLocked = LockMouseInWindow;
            }
        }
    }
}

#endif