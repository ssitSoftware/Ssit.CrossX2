using Ssit.CrossX2.Framework.Games.Logic;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.BotBrains;

public class WanderBehavior(float aheadProbeDistance, float groundProbeDistance): Behavior<IBrainCharacter>
{
    protected override void OnLeaveState(IBrainCharacter obj)
    {
        base.OnLeaveState(obj);
        obj.InputController.ResetManualInput();
    }

    protected override bool OnUpdate(IBrainCharacter obj, float dt)
    {
        if (IsObstacleAhead(obj, obj.FaceLeft))
        {
            var turned = !obj.FaceLeft;
            if (IsObstacleAhead(obj, turned))
            {
                // Blocked both ways (e.g. a narrow alcove) - stay put instead of
                // flipping FaceLeft every tick, which would cancel out movement.
                obj.InputController.SetValue(SteeringControlNames.HorizontalMove, 0f);
                return false;
            }

            obj.FaceLeft = turned;
        }

        obj.InputController.SetValue(SteeringControlNames.HorizontalMove, obj.FaceLeft ? -1f : 1f);
        obj.InputController.SetButtonState(SteeringControlNames.Walk, ButtonState.Down);
        return false;
    }

    private bool IsObstacleAhead(IBrainCharacter obj, bool faceLeft)
    {
        var body = obj.Body;
        var aabb = body.Colliders[0].Aabb;
        var stepUpTolerance = body.StepUpTolerance;

        var wallBottom = aabb.Bottom - (stepUpTolerance + float.Epsilon);
        var wallProbe = faceLeft
            ? new Aabb(aabb.Left - aheadProbeDistance, aabb.Top, aabb.Left - 0.01f, wallBottom)
            : new Aabb(aabb.Right + 0.01f, aabb.Top, aabb.Right + aheadProbeDistance, wallBottom);

        if (HasAnyObstacle(body, wallProbe))
        {
            return true;
        }

        var groundProbe = faceLeft
            ? new Aabb(aabb.Left - groundProbeDistance, aabb.Bottom - float.Epsilon, aabb.Left - groundProbeDistance + 0.5f, aabb.Bottom + stepUpTolerance)
            : new Aabb(aabb.Right + groundProbeDistance - 0.5f, aabb.Bottom - float.Epsilon, aabb.Right + groundProbeDistance, aabb.Bottom + stepUpTolerance);

        return !HasAnyObstacle(body, groundProbe);
    }

    private static bool HasAnyObstacle(IBody body, Aabb probe)
    {
        var simulation = body.Simulation;

        if (simulation.GetColliders(probe, body, IMaterial.AllColliders, colliderType: ColliderType.Static).Count > 0)
        {
            return true;
        }

        var collidesWith = body.Colliders[0].Material.CollidesWith;
        return simulation.GetColliders(probe, body, collidesWith, colliderType: ColliderType.Dynamic).Count > 0;
    }
}