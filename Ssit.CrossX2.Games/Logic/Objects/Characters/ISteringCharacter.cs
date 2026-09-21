using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;

public interface ISteeringCharacter: ICharacter, IGameObject
{
    ISteeringInput SteeringInput { get; }
    CharacterSteeringParameters SteeringParameters { get; }
    ICharacterPhysicsValues PhysicsValues { get; }
    SteeringState<ISteeringCharacter> CurrentSteeringState { get; }
    void SetSteeringState(string name);
}