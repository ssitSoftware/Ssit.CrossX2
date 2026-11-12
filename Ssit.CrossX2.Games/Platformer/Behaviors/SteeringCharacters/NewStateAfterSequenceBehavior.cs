using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Switches to a new steering state when the current sprite sequence has finished.
/// </summary>
public class NewStateAfterSequenceBehavior(string newSequenceName) : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnSequenceFinished(ISteeringCharacter obj, string name)
    {
        obj.SetSteeringState(newSequenceName);
        return true;
    }
}
