using System.Numerics;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.AabbPhysics;

internal static class MovementCollisionCalculator
{
    public const float MovementEpsilon = 0.001f;

    private static readonly List<ICollider> CollidersToTest = new();

    // TODO: Change way of returning collider - not horizontal and vertical but one and return flag of collision: None, Horizontal, Vertical, Other
    public static bool GetMovementCollisions(Simulation simulation, ColliderType colliderType, Body body, int colliderIndex, ref Vector2 move, ref Vector2 normal, ref Vector2 friction, out ICollider horizontalMovementCollider, out ICollider verticalMovementCollider)
    {
        var objCollider = body.Colliders[colliderIndex];
        var movementAabb = objCollider.Aabb.Union(objCollider.Aabb.GetOffset(move));
        movementAabb.Inflate(MovementEpsilon);

        var colliderGroup = objCollider.Material.ColliderGroup;

        horizontalMovementCollider = null;
        verticalMovementCollider = null;

        CollidersToTest.Clear();
        
        simulation.GetColliders(movementAabb, CollidersToTest);
        
        for (var idx = 0; idx < CollidersToTest.Count; ++idx)
        {
            var collider = CollidersToTest[idx];

            if (!collider.IsActive) continue;
            if (body == collider.AttachedBody) continue;
            
            if ((collider.Type & colliderType) == 0) continue;
            if ((colliderGroup & collider.Material.ColliderGroup) == 0) continue;
            
            if (!objCollider.GetMovementCollision(collider, ref move, out var collisionNormal)) continue;

            if (Math.Abs(collisionNormal.X) > float.Epsilon)
            {
                horizontalMovementCollider = collider;
                friction.Y = Math.Abs(collisionNormal.X) * collider.Material.Friction;
                normal.X = collisionNormal.X * collider.Material.Bounce;
            }

            if (Math.Abs(collisionNormal.Y) > float.Epsilon)
            {
                verticalMovementCollider = collider;
                friction.X = Math.Abs(collisionNormal.Y) * collider.Material.Friction;
                normal.Y = collisionNormal.Y * collider.Material.Bounce;
            }

            movementAabb = objCollider.Aabb.GetOffset(body.Position).Union(objCollider.Aabb.GetOffset(body.Position + move));
            movementAabb.Inflate(MovementEpsilon);
        }

        return horizontalMovementCollider != null || verticalMovementCollider != null;
    }
}