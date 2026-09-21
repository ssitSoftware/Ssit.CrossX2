using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;

public interface ISteeringInput
{
    ButtonState Button(string id);
    float Value(string id);
}