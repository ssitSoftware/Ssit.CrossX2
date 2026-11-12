using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Switches to the fall state when the current (jump) animation sequence finishes.
/// </summary>
public class JumpToFallBehavior(string fallState = "Fall") : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnSequenceFinished(ISteeringCharacter obj, string name)
    {
        obj.SetSteeringState(fallState);
        return true;
    }
}