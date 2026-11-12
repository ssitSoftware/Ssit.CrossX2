using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.BotBrains;

/// <summary>
/// Drops the target and switches to lostTargetStateName when the target gets farther away than maxDistance.
/// </summary>
public class LoseTargetBehavior(float maxDistance, string lostTargetStateName) : Behavior<IBrainCharacter>
{
    protected override bool OnUpdate(IBrainCharacter obj, float dt)
    {
        var parameters = obj.GetParameters<AttackTargetParameters>();
        var target = parameters.Target;

        if (target is not { Alive: true })
            return false;

        if (Vector2.Distance(target.Position, obj.Body.Position) <= maxDistance)
            return false;

        parameters.Target = null;
        obj.SetBrainState(lostTargetStateName);
        return true;
    }
}
