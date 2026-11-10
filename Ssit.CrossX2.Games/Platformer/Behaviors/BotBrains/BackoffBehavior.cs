using Ssit.CrossX2.Framework.Games.Logic;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.BotBrains;

/// <param name="duration">Minimum backoff time.</param>
/// <param name="chaseStateName">State to return to once backed off.</param>
/// <param name="lostTargetStateName">State when the target is gone or given up on.</param>
/// <param name="minDistance">Keep backing off until at least this far (horizontally) from the target - prevents
/// rapid Chasing/Backoff flipping when the target stays close, e.g. on a platform right above.</param>
/// <param name="maxDuration">Backoff ends after this time even if <paramref name="minDistance"/> wasn't reached (e.g. blocked by a wall).</param>
/// <param name="maxBackoffs">Give up after this many backoffs in a row without attacking (target is unreachable). 0 = never.</param>
/// <param name="ignoreTime">After giving up, the target is ignored by scanning and alerts for this long.</param>
public class BackoffBehavior(float duration, string chaseStateName, string lostTargetStateName,
    float minDistance = 0f, float maxDuration = float.MaxValue, int maxBackoffs = 0, float ignoreTime = 4f) : Behavior<IBrainCharacter>
{
    private class Parameters
    {
        public float ElapsedTime;
    }

    protected override void OnEnterState(IBrainCharacter obj)
    {
        obj.GetParameters<Parameters>().ElapsedTime = 0;
        obj.GetParameters<AttackTargetParameters>().BackoffCount++;
    }

    protected override bool OnUpdate(IBrainCharacter obj, float dt)
    {
        var targetParameters = obj.GetParameters<AttackTargetParameters>();
        var target = targetParameters.Target;
        if (target is not { Alive: true })
        {
            obj.SetBrainState(lostTargetStateName);
            return true;
        }

        if (maxBackoffs > 0 && targetParameters.BackoffCount > maxBackoffs)
        {
            // Keeps ending up too close without ever getting an attack in - the target is unreachable.
            targetParameters.GiveUpTarget(ignoreTime);
            obj.SetBrainState(lostTargetStateName);
            return true;
        }

        var parameters = obj.GetParameters<Parameters>();
        parameters.ElapsedTime += dt;

        var farEnough = MathF.Abs(target.Position.X - obj.Body.Position.X) >= minDistance;
        if ((parameters.ElapsedTime >= duration && farEnough) || parameters.ElapsedTime >= maxDuration)
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
