namespace Ssit.CrossX2.Framework.Input;

public interface IVirtualGameInput
{
    ButtonState GetButton(GameControllerButton button);
    float GetAxis(GameControllerAxis axis);
    
    void SetButton(GameControllerButton button, ButtonState state);
    void SetAxis(GameControllerAxis axis, float value);
}