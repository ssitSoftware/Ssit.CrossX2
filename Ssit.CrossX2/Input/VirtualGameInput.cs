namespace Ssit.CrossX2.Input;

internal class VirtualGameInput : IVirtualGameInput
{
    private readonly Dictionary<GameControllerButton, ButtonState> _buttonStates = new();
    private readonly Dictionary<GameControllerAxis, float> _axesValues = new();
    
    public ButtonState GetButton(GameControllerButton button) => _buttonStates.GetValueOrDefault(button, ButtonState.Empty);

    public float GetAxis(GameControllerAxis axis) => _axesValues.GetValueOrDefault(axis, 0);

    public void SetButton(GameControllerButton button, ButtonState state) => _buttonStates[button] = state;

    public void SetAxis(GameControllerAxis axis, float value) => _axesValues[axis] = value;
}