using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// When the current sequence finishes, goes back to "Run" if on the ground, otherwise to "Fall".
/// </summary>
public class ReturnToRunOrFallOnFinishedBehavior : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnSequenceFinished(ISteeringCharacter obj, string name)
    {
        obj.SetSteeringState(obj.SteeringParameters.IsOnGround ? "Run" : "Fall");
        return true;
    }
}