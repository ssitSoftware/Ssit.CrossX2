using Ssit.CrossX2.Framework.Games.Logic;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;
using Ssit.CrossX2.Framework.Games.Platformer.Helpers;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.BotBrains;

/// <summary>
/// Patrol: walks in the facing direction and turns around at walls or ledges.
/// aheadProbeDistance is how far ahead walls are checked, groundProbeDistance how far ahead there must be ground.
/// </summary>
public class WanderBehavior(float aheadProbeDistance, float groundProbeDistance): Behavior<IBrainCharacter>
{
    protected override void OnLeaveState(IBrainCharacter obj)
    {
        base.OnLeaveState(obj);
        // Release movement/walk input so it does not leak into the next state
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
        // Wander at walk speed
        obj.InputController.SetButtonState(SteeringControlNames.Walk, ButtonState.Down);
        return false;
    }

    /// <summary>
    /// True if walking in the given direction is blocked by a wall (ignoring small steps) or leads off a ledge.
    /// </summary>
    private bool IsObstacleAhead(IBrainCharacter obj, bool faceLeft)
    {
        var body = obj.Body;
        var aabb = body.Colliders[0].Aabb;
        var stepUpTolerance = body.StepUpTolerance;

        // Probe stops above StepUpTolerance so walkable steps are not treated as walls
        var wallBottom = aabb.Bottom - (stepUpTolerance + float.Epsilon);
        var wallProbe = faceLeft
            ? new Aabb(aabb.Left - aheadProbeDistance, aabb.Top, aabb.Left - 0.01f, wallBottom)
            : new Aabb(aabb.Right + 0.01f, aabb.Top, aabb.Right + aheadProbeDistance, wallBottom);

        if (HasAnyObstacle(body, wallProbe, c => c.BlocksHorizontalMove(faceLeft)))
        {
            return true;
        }

        // Small box at foot level groundProbeDistance ahead - no ground there means a ledge
        var groundProbe = faceLeft
            ? new Aabb(aabb.Left - groundProbeDistance, aabb.Bottom - float.Epsilon, aabb.Left - groundProbeDistance + 0.5f, aabb.Bottom + stepUpTolerance)
            : new Aabb(aabb.Right + groundProbeDistance - 0.5f, aabb.Bottom - float.Epsilon, aabb.Right + groundProbeDistance, aabb.Bottom + stepUpTolerance);

        return !HasAnyObstacle(body, groundProbe, c => c.IsGround());
    }

    /// <summary>
    /// Checks static colliders, then dynamic ones the body collides with (e.g. moving platforms, other characters).
    /// </summary>
    private static bool HasAnyObstacle(IBody body, Aabb probe, Func<ICollider, bool> filter)
    {
        var simulation = body.Simulation;

        if (simulation.GetColliders(probe, body, IMaterial.AllColliders, colliderType: ColliderType.Static).Any(filter))
        {
            return true;
        }

        var collidesWith = body.Colliders[0].Material.CollidesWith;
        return simulation.GetColliders(probe, body, collidesWith, colliderType: ColliderType.Dynamic).Any(filter);
    }
}