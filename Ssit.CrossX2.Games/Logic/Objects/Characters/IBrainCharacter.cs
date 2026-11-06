namespace Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;

public interface IBrainCharacter
{
    ISteeringInputController InputController { get; }
    void SetBrainState(string state);
}