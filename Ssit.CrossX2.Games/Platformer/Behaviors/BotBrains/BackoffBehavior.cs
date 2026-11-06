using Ssit.CrossX2.Framework.Games.Logic;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.BotBrains;

public class BackoffBehavior(float duration, string chaseStateName, string lostTargetStateName) : Behavior<IBrainCharacter>
{
    private class Parameters
    {
        public float RemainingTime;
    }

    protected override void OnEnterState(IBrainCharacter obj)
    {
        obj.GetParameters<Parameters>().RemainingTime = duration;
    }

    protected override bool OnUpdate(IBrainCharacter obj, float dt)
    {
        var target = obj.GetParameters<AttackTargetParameters>().Target;
        if (target is not { Alive: true })
        {
            obj.SetBrainState(lostTargetStateName);
            return true;
        }

        var parameters = obj.GetParameters<Parameters>();
        parameters.RemainingTime -= dt;
        if (parameters.RemainingTime <= 0)
        {
            obj.SetBrainState(chaseStateName);
            return true;
        }

        var movingLeft = target.Position.X >= obj.Body.Position.X;
        obj.FaceLeft = !movingLeft;
        obj.InputController.SetValue(SteeringControlNames.HorizontalMove, movingLeft ? -1f : 1f);
        return false;
    }
}
