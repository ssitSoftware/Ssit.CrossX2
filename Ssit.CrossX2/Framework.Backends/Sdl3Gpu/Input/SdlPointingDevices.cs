using System.Numerics;
using SDL;
using Ssit.CrossX2.Framework.Input;
using Ssit.CrossX2.Framework.Input.Internal;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Input;

internal unsafe partial class SdlPointingDevices : PointingDevicesBase
{
    public override PointingDevicesMode Mode
    {
        get => base.Mode;
        set
        {
            var val = value;
            
            var platform = SDL_GetPlatform()?.ToLowerInvariant();
            if (platform is not ("android" or "ios"))
            {
                val &= ~PointingDevicesMode.Touch;
            }
            
            base.Mode = val;
        }
    }

    private SDL_MouseButtonFlags _previousFlags = 0;
    private Vector2 _previousMousePosition;
    private bool _mouseInWindowLocked;

    private readonly List<SDL_FingerID> _fingers = [];
    private readonly SdlHandle<SDL_Window> _windowHandle;

    public SdlPointingDevices(SdlHandle<SDL_Window> windowHandle): this()
    {
        _windowHandle = windowHandle;
    }

    public void Update()
    {
        OnPreUpdate();

        AnalyzeMouse();

        if ((Mode & PointingDevicesMode.Touch) != 0)
        {
            ProcessTouch();
        }
    }

    protected override void ShowPointer(bool show)
    {
        if (show)
        {
            SDL_ShowCursor();
        }
        else
        {
            SDL_HideCursor();
        }
    }

    private void ProcessButton(ButtonState state, int id, bool moved, Vector2 position)
    {
        if (!state.IsChanged && !moved)
            return;
        
        if (!state.IsChanged)
        {
            if (moved && state.IsDown)
            {
                SetPointer(id, ButtonState.Down, position);
            }
        }
        else
        {
            if (state.IsDown)
            {
                SetPointer(id, ButtonState.JustPressed, position);
            }
            else
            {
                SetPointer(id, ButtonState.JustReleased, position);
            }
        }
    }
}