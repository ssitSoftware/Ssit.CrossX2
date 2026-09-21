using SDL;
using Ssit.CrossX2.Framework.Input;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Input;

internal unsafe class SdlGameController: IDisposable
{
    private readonly int _playerIndex;
    private SdlHandle<SDL_Gamepad> _handle;
    
    private static readonly HashSet<uint> AttachedControllers = new();
    private readonly HashSet<GameControllerButton> _previousButtons = new();

    public SdlGameController(int playerIndex)
    {
        _playerIndex = playerIndex;
        SDL_FindController();
    }

    public bool IsConnected => SDL_GetGamepadFromPlayerIndex(_playerIndex) != null;

    public float GetAxis(GameControllerAxis axis)
    {
        var gamepad = _handle != null ? _handle.Pointer : null;

        if (gamepad is null)
        {
            return 0;
        }
        
        float value = 0;
        
        if (axis == GameControllerAxis.DPadX)
        {
            value += SDL_GetGamepadButton(gamepad,  SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_LEFT) == true ? -1 : 0;
            value += SDL_GetGamepadButton(gamepad,  SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_RIGHT) == true ? 1 : 0;
            return value;
        }
        
        if (axis == GameControllerAxis.DPadY)
        {
            value += SDL_GetGamepadButton(gamepad,  SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_UP) == true ? -1 : 0;
            value += SDL_GetGamepadButton(gamepad,  SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_DOWN) == true ? 1 : 0;
            return value;
        }
        
        value = SDL_GetGamepadAxis(gamepad, (SDL_GamepadAxis)axis);
        return value / short.MaxValue;
    }

    private bool IsButtonDown(GameControllerButton button)
    {
        if (button < GameControllerButton.BasicMax)
        {
            var gamepad = _handle != null ? _handle.Pointer : null;

            if (gamepad is null)
            {
                return false;
            }
            
            return SDL_GetGamepadButton(gamepad, (SDL_GamepadButton)button) == true;
        }
        
        const float minAxisValueForButton = 0.85f;

        switch (button)
        {
            case GameControllerButton.LeftStickLeft:
                return GetAxis(GameControllerAxis.LeftX) < -minAxisValueForButton;
            
            case GameControllerButton.LeftStickRight:
                return GetAxis(GameControllerAxis.LeftX) > minAxisValueForButton;
            
            case GameControllerButton.LeftStickUp:
                return GetAxis(GameControllerAxis.LeftY) < -minAxisValueForButton;
            
            case GameControllerButton.LeftStickDown:
                return GetAxis(GameControllerAxis.LeftY) > minAxisValueForButton;
            
            case GameControllerButton.RightStickLeft:
                return GetAxis(GameControllerAxis.RightX) < -minAxisValueForButton;
            
            case GameControllerButton.RightStickRight:
                return GetAxis(GameControllerAxis.RightX) > minAxisValueForButton;
            
            case GameControllerButton.RightStickUp:
                return GetAxis(GameControllerAxis.RightY) < -minAxisValueForButton;
            
            case GameControllerButton.RightStickDown:
                return GetAxis(GameControllerAxis.RightY) > minAxisValueForButton;
            
            case GameControllerButton.LeftTrigger:
                return GetAxis(GameControllerAxis.LeftTrigger) > minAxisValueForButton;
            
            case GameControllerButton.RightTrigger:
                return GetAxis(GameControllerAxis.RightTrigger) > minAxisValueForButton;
        }
        
        return false;
    }
    
    public ButtonState GetButton(GameControllerButton button)
    {
        if ( button == GameControllerButton.None)
            return ButtonState.Empty;
        
        bool isDown = IsButtonDown(button);
        bool wasDown = _previousButtons.Contains(button);

        return new ButtonState(isDown, isDown != wasDown);
    }
    
    public void PostUpdate()
    {
        _previousButtons.Clear();
        for (var idx = 0; idx < (int) GameControllerButton.Max; ++idx)
        {
            if (IsButtonDown((GameControllerButton) idx))
            {
                _previousButtons.Add((GameControllerButton)idx);
            }
        }
    }

    private void CloseHandle()
    {
        var gamepad = _handle != null ? _handle.Pointer : null;
        if (gamepad != null)
        {
            SDL_CloseGamepad(gamepad);
            _handle = null;
        }
    }
    
    public void Dispose() => CloseHandle();

    public void ProcessEvent(SDL_Event sdlEvent)
    {
        switch ((SDL_EventType)sdlEvent.type)
        {
            case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:
                if (_handle == null || _handle.Pointer == null)
                {
                    SDL_FindController();
                }
                break;
            
            case SDL_EventType.SDL_EVENT_GAMEPAD_REMOVED:

                if (_handle != null && _handle.Pointer != null)
                {
                    if (SDL_GetGamepadConnectionState(_handle.Pointer) <= 0)
                    {
                        CloseHandle();
                    }
                }
                break;
        }
    }
    
    private void SDL_FindController()
    {
        int count;
        var joysticks = SDL_GetJoysticks(&count);

        try
        {
            for (int i = 0; i < count; i++)
            {
                if (SDL_IsGamepad(joysticks[i]) == true)
                {
                    if (!AttachedControllers.Add((uint)joysticks[i]))
                        continue;

                    var gp = SDL_OpenGamepad(joysticks[i]);
                    SDL_SetGamepadPlayerIndex(gp, _playerIndex);
                    _handle = new SdlHandle<SDL_Gamepad>(gp);
                    break;
                }
            }
        }
        finally
        {
            SDL_free(joysticks);
        }
    }

    public void Vibrate(Vibration low, Vibration high, uint ms)
    {
        var gamepad = _handle != null ? _handle.Pointer : null;

        if (gamepad is null)
        {
            return;
        }

        var lowVib = (int)low * 0xffff / 10;
        var highVib = (int)high * 0xffff / 10;
        
        SDL_RumbleGamepad(gamepad, (ushort)lowVib, (ushort)highVib, ms);
    }
}