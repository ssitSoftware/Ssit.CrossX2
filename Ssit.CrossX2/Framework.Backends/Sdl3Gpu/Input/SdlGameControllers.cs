using SDL;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Input;

internal class SdlGameControllers: IGameControllers
{
    private readonly HashSet<int> _switchButtons = new();
    
    public void SwitchButtons(int player, bool @switch)
    {
        if (@switch)
        {
            _switchButtons.Add(player);
        }
        else
        {
            _switchButtons.Remove(player);
        }
    }

    public byte VibrationForce { get; set; }

    private readonly SdlGameController[] _controllers =
    [
        new(0),
        new(1),
        new(2),
        new(3)
    ];
    
    public ButtonState GetButton(int player, GameControllerButton button)
    {
        if (_switchButtons.Contains(player))
        {
            switch (button)
            {
                case GameControllerButton.A:
                    button = GameControllerButton.B;
                    break;
                
                case GameControllerButton.B:
                    button = GameControllerButton.A;
                    break;
                
                case GameControllerButton.X:
                    button = GameControllerButton.Y;
                    break;
                
                case GameControllerButton.Y:
                    button = GameControllerButton.X;
                    break;
            }
        }
        
        return _controllers[player].GetButton(button);
    }

    public float GetAxis(int player, GameControllerAxis axis)
    {
        return _controllers[player].GetAxis(axis);
    }

    public bool IsConnected(int player)
    {
        return _controllers[player].IsConnected;
    }

    public void Vibrate(int player, Vibration low, Vibration high, uint ms)
    {
        _controllers[player].Vibrate(low, high, ms);
    }

    public void PostUpdate()
    {
        foreach (var controller in _controllers)
        {
            controller.PostUpdate();
        }
    }

    public void ProcessEvent(SDL_Event e)
    {
        foreach (var controller in _controllers)
        {
            controller.ProcessEvent(e);
        }
    }
}