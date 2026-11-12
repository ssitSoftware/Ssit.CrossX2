namespace Ssit.CrossX2.Framework.Input;

/// <summary>
/// Provides readable and writable virtual game controller input state.
/// </summary>
public interface IVirtualGameInput
{
    ButtonState GetButton(GameControllerButton button);
    float GetAxis(GameControllerAxis axis);
    
    void SetButton(GameControllerButton button, ButtonState state);
    void SetAxis(GameControllerAxis axis, float value);
}
