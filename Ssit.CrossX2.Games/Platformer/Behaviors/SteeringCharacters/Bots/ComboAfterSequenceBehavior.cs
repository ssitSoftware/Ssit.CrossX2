using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Utils;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Bots;

/// <summary>
/// When the current (attack) sequence finishes, follows up with <paramref name="comboState"/> with the given chance,
/// otherwise goes to <paramref name="fallbackState"/>.
/// </summary>
public class ComboAfterSequenceBehavior(string comboState, string fallbackState, float comboChance) : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnSequenceFinished(ISteeringCharacter obj, string name)
    {
        obj.SetSteeringState(MersenneTwister.Shared.NextSingle() < comboChance ? comboState : fallbackState);
        return true;
    }
}
