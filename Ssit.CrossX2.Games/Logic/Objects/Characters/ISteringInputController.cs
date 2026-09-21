using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;

public interface ISteeringInputController : ISteeringInput
{
    void SetValue(string id, float value);
    void SetButtonState(string id, ButtonState buttonState);
}