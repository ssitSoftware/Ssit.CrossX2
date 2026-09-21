namespace Ssit.CrossX2.Framework.Input.Internal;

public abstract class GameControllersBase: IGameControllers
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

    public byte VibrationForce { get; set; } = 3;

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
        
        return GetButtonInternal(player, button);   
    }
    public float GetAxis(int player, GameControllerAxis axis) => GetAxisInternal(player, axis);
    public bool IsConnected(int player) => IsConnectedInternal(player);

    public void Vibrate(int player, Vibration low, Vibration high, uint ms)
    {
        if (VibrationForce > 0)
        {
            VibrateInternal(player, (ushort) ((int) low * 0xffff * VibrationForce / 40), (ushort) ((int) high * 0xffff * VibrationForce / 40), ms);
        }
    }

    protected abstract ButtonState GetButtonInternal(int player, GameControllerButton button);
    protected abstract float GetAxisInternal(int player, GameControllerAxis axis);
    protected abstract void VibrateInternal(int player, ushort low, ushort high, uint ms);
    protected abstract bool IsConnectedInternal(int player);
}