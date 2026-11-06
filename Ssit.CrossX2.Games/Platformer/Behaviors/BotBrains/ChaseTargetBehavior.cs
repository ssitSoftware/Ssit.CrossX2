using Ssit.CrossX2.Framework.Games.Logic;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.BotBrains;

public class ChaseTargetBehavior(float tooCloseDistance, string backoffStateName, string lostTargetStateName) : Behavior<IBrainCharacter>
{
    protected override bool OnUpdate(IBrainCharacter obj, float dt)
    {
        var parameters = obj.GetParameters<AttackTargetParameters>();
        var target = parameters.Target;

        if (target is not { Alive: true })
        {
            parameters.Target = null;
            obj.SetBrainState(lostTargetStateName);
            return true;
        }

        var toTarget = target.Position.X - obj.Body.Position.X;
        if (MathF.Abs(toTarget) < tooCloseDistance)
        {
            // Target is overlapping/too close to reliably tell which side it's on -
            // back off instead of flipping FaceLeft every tick.
            obj.SetBrainState(backoffStateName);
            return true;
        }

        obj.FaceLeft = toTarget < 0;
        obj.InputController.SetValue(SteeringControlNames.HorizontalMove, obj.FaceLeft ? -1f : 1f);
        return false;
    }
}
