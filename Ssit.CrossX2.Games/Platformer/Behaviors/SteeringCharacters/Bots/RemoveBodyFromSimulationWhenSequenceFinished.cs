using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Bots;

/// <summary>
/// Removes the character's body from the physics simulation when the current sequence ends (e.g. after a death animation).
/// </summary>
public class RemoveBodyFromSimulationWhenSequenceFinished: SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnSequenceFinished(ISteeringCharacter obj, string name)
    {
        obj.Body.Simulation.RemoveBody(obj.Body);
        return true;
    }
}