#if IOS || ANDROID

using System.Numerics;
using SDL;
using Ssit.CrossX2.Framework.Input;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Input;

internal unsafe partial class SdlPointingDevices
{
    private readonly List<SDL_TouchID> _touchDevices = [];

    private SdlPointingDevices()
    {
        using var array = SDL_GetTouchDevices();
        if (array is not null)
        {
            for (var idx = 0; idx < array.Count; idx++)
            {
                var type = SDL_GetTouchDeviceType(array[idx]);
                    
                if (type == SDL_TouchDeviceType.SDL_TOUCH_DEVICE_DIRECT)
                {
                    _touchDevices.Add(array[idx]);
                }
            }
        }
    }

    private void AnalyzeMouse()
    {
    }

    private void ProcessTouch()
    {
        int width, height;
        SDL_GetWindowSizeInPixels(_windowHandle.Pointer, &width, &height);
        
        if (_touchDevices.Count == 0)
            return;
        
        for (var idx =0; idx < _fingers.Count;)
        {
            var pointerId = GetTouchId((ulong)_fingers[idx]);
            var pointer = GetPointer(pointerId);
            
            if (pointer != null)
            {
                if (pointer.State.IsDown)
                { 
                    SetPointer(pointerId, ButtonState.JustReleased, null);
                }
                ++idx;
            }
            else
            {
                _fingers.RemoveAt(idx);
            }
        }

        foreach (var touchDeviceId in _touchDevices)
        {
            using var array = SDL_GetTouchFingers(touchDeviceId);
            if (array != null)
            {
                for (var idx = 0; idx < array.Count; idx++)
                {
                    var finger = array[idx];
                    var id = GetTouchId((ulong)finger.id);

                    var pointer = GetPointer(id);
                    if (pointer == null)
                    {
                        SetPointer(id, ButtonState.JustPressed, new Vector2(finger.x * width, finger.y * height));
                        _fingers.Add(finger.id);
                    }
                    else
                    {
                        SetPointer(id, ButtonState.Down, new Vector2(finger.x * width, finger.y * height));
                    }
                }
            }
        }
    }
}

#endif