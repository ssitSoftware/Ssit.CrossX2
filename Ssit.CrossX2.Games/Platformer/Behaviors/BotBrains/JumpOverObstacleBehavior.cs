using Ssit.CrossX2.Framework.Games.Logic;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.BotBrains;

public class JumpOverObstacleBehavior(float probeDistance, string lostTargetStateName, float jumpableHeightFraction = 1f) : Behavior<IBrainCharacter>
{
    protected override bool OnUpdate(IBrainCharacter obj, float dt)
    {
        obj.InputController.SetButtonState(SteeringControlNames.Jump, ButtonState.Empty);
        
        var body = obj.Body;
        var aabb = body.Colliders[0].Aabb;
        var stepUpTolerance = body.StepUpTolerance;
        var faceLeft = obj.FaceLeft;

        var probe = faceLeft
            ? new Aabb(aabb.Left - probeDistance, aabb.Top, aabb.Left - 0.01f, aabb.Bottom - float.Epsilon)
            : new Aabb(aabb.Right + 0.01f, aabb.Top, aabb.Right + probeDistance, aabb.Bottom - float.Epsilon);

        var collidersByDistance = body.Simulation.GetColliders(probe, body, IMaterial.AllColliders, colliderType: ColliderType.Static)
            .OrderBy(c => faceLeft ? aabb.Left - c.Aabb.Right : c.Aabb.Left - aabb.Right);

        // Walk the probed colliders from nearest to farthest, simulating the step-by-step
        // climb: each rise within StepUpTolerance of the previous step is auto-walkable
        // (stairs/slope), so only a rise bigger than that counts as a real obstacle to jump.
        var stepTop = aabb.Bottom;
        var obstacleTop = float.MaxValue;
        var obstacleNearEdge = 0f;
        foreach (var collider in collidersByDistance)
        {
            if (collider.Aabb.Top >= stepTop - stepUpTolerance - float.Epsilon)
            {
                stepTop = collider.Aabb.Top;
                continue;
            }

            obstacleTop = collider.Aabb.Top;
            obstacleNearEdge = faceLeft ? collider.Aabb.Right : collider.Aabb.Left;
            break;
        }

        if (obstacleTop >= float.MaxValue)
            return false;

        var jumpableTop = aabb.Bottom - aabb.Height * jumpableHeightFraction;
        if (obstacleTop > jumpableTop)
        {
            obj.InputController.SetButtonState(SteeringControlNames.Jump, ButtonState.JustPressed);
            return false;
        }

        // Obstacle is too tall to clear - if the target is beyond it, it's unreachable.
        var parameters = obj.GetParameters<AttackTargetParameters>();
        var target = parameters.Target;
        if (target is not { Alive: true })
            return false;

        var targetNearEdge = (target as ISteeringCharacter)?.Body.Colliders[0].Aabb is { } targetAabb
            ? faceLeft ? targetAabb.Right : targetAabb.Left
            : target.Position.X;

        var targetBeyondObstacle = faceLeft ? targetNearEdge < obstacleNearEdge : targetNearEdge > obstacleNearEdge;
        if (!targetBeyondObstacle)
            return false;

        parameters.Target = null;
        obj.SetBrainState(lostTargetStateName);
        return true;
    }
}
