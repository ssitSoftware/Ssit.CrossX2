using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Switches to <paramref name="newStateName"/> after <paramref name="time"/> seconds in the state.
/// Use instead of <see cref="NewStateAfterSequenceBehavior"/> when the state shares its sprite sequence with the
/// previous state (the sequence is not restarted then, so "sequence finished" would come at a random moment).
/// Keeps the timer in the instance - create a separate instance per object.
/// </summary>
public class NewStateAfterTimeBehavior(string newStateName, float time) : SteeringBehavior<ISteeringCharacter>
{
    private float _elapsed;

    protected override void OnEnter(ISteeringCharacter character)
    {
        _elapsed = 0;
    }

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        _elapsed += dt;

        if (_elapsed < time)
            return false;

        obj.SetSteeringState(newStateName);
        return true;
    }
}
