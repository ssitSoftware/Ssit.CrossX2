using System.Numerics;
using Ssit.CrossX2.Framework.Games.AabbPhysics.Colliders;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.AabbPhysics.Algorithms;

internal static class RectRectCollision
{
    public static bool Check(RectCollider rectCollider1, RectCollider rectCollider2)
    {
        return rectCollider1.Aabb.Intersects(rectCollider2.Aabb);
    }

    public static bool GetMovementCollision(RectCollider movingCollider, RectCollider obstacle, ref Vector2 move, out Vector2 normal)
    {
        var collision = false;

        var o = obstacle.Aabb;
        var b = movingCollider.Aabb;
        var a = movingCollider.GetAabb(movingCollider.AttachedBody.Position + move);

        var newNormal = Vector2.Zero;
        var obstacleSides = obstacle.Material.Sides;
        var sides = movingCollider.Material.Sides;

        if ((obstacleSides.HasFlag(ColliderSides.Left) && sides.HasFlag(ColliderSides.Right) && move.X > 0) ||
            (obstacleSides.HasFlag(ColliderSides.Right) && sides.HasFlag(ColliderSides.Left) && move.X < 0))
        {
            collision |= AnalyzeHorizontalMovementCollision(ref a, ref b, ref o, ref move, ref newNormal);
        }

        if ((obstacleSides.HasFlag(ColliderSides.Top) && sides.HasFlag(ColliderSides.Bottom) && move.Y > 0) ||
            (obstacleSides.HasFlag(ColliderSides.Bottom) && sides.HasFlag(ColliderSides.Top) && move.Y < 0))
        {
            collision |= AnalyzeVerticalMovementCollision(ref a, ref b, ref o, ref move, ref newNormal);
        }

        normal = newNormal;
        return collision;
    }

    private static bool AnalyzeVerticalMovementCollision(ref Aabb after, ref Aabb before, ref Aabb obstacle, ref Vector2 move, ref Vector2 normal)
    {
        // Already touching the obstacle from the side (e.g. sliding along a tiled wall) - it is beside, not above / below.
        // Without this the top / bottom edge of the next wall tile works as a floor / ceiling and stops the slide.
        if (IsTouchingHorizontally(ref before, ref obstacle))
            return false;

        if (before.Bottom - MovementCollisionCalculator.MovementEpsilon < obstacle.Top && after.Bottom + MovementCollisionCalculator.MovementEpsilon > obstacle.Top)
        {
            var movey = obstacle.Top - before.Bottom;
            var testMove = move * (movey / move.Y);
            var test = before.GetOffset(testMove);

            if (test.Right > obstacle.Left && test.Left < obstacle.Right)
            {
                move.Y = testMove.Y;
                normal = new Vector2(0, -1);
                return true;
            }
        }

        if (before.Top + MovementCollisionCalculator.MovementEpsilon > obstacle.Bottom && after.Top - MovementCollisionCalculator.MovementEpsilon < obstacle.Bottom)
        {
            var movey = obstacle.Bottom - before.Top;
            var testMove = move * (movey / move.Y);
            var test = before.GetOffset(testMove);

            if (test.Right > obstacle.Left && test.Left < obstacle.Right)
            {
                move.Y = testMove.Y;
                normal = new Vector2(0, 1);
                return true;
            }
        }

        return false;
    }

    private static bool AnalyzeHorizontalMovementCollision(ref Aabb after, ref Aabb before, ref Aabb obstacle, ref Vector2 move, ref Vector2 normal)
    {
        // Already touching the obstacle from above / below (e.g. sliding along a tiled ceiling or floor) - it is above / below,
        // not beside. Without this the side edge of the next ceiling tile works as a wall when the move goes a bit into
        // the ceiling (flyers pressed into a wall-ceiling corner could not slide out).
        if (IsTouchingVertically(ref before, ref obstacle))
            return false;

        if (before.Right - MovementCollisionCalculator.MovementEpsilon < obstacle.Left && after.Right + MovementCollisionCalculator.MovementEpsilon > obstacle.Left)
        {
            var movex = obstacle.Left - before.Right;
            var testMove = move * (movex / move.X);
            var test = before.GetOffset(testMove);

            if (test.Bottom > obstacle.Top && test.Top < obstacle.Bottom)
            {
                move.X = testMove.X;
                normal = new Vector2(-1, 0);
                return true;
            }
        }

        if (before.Left + MovementCollisionCalculator.MovementEpsilon > obstacle.Right && after.Left - MovementCollisionCalculator.MovementEpsilon < obstacle.Right)
        {
            var movex = obstacle.Right - before.Left;
            var testMove = move * (movex / move.X);
            var test = before.GetOffset(testMove);

            if (test.Bottom > obstacle.Top && test.Top < obstacle.Bottom)
            {
                move.X = testMove.X;
                normal = new Vector2(1, 0);
                return true;
            }
        }

        return false;
    }

    /// <summary>The rect touches the obstacle's bottom or top face (within the movement epsilon) - it is right below / above it.</summary>
    private static bool IsTouchingVertically(ref Aabb rect, ref Aabb obstacle) =>
        MathF.Abs(rect.Bottom - obstacle.Top) <= MovementCollisionCalculator.MovementEpsilon ||
        MathF.Abs(rect.Top - obstacle.Bottom) <= MovementCollisionCalculator.MovementEpsilon;

    /// <summary>The rect touches the obstacle's left or right face (within the movement epsilon) - it is right beside it.</summary>
    private static bool IsTouchingHorizontally(ref Aabb rect, ref Aabb obstacle) =>
        MathF.Abs(rect.Right - obstacle.Left) <= MovementCollisionCalculator.MovementEpsilon ||
        MathF.Abs(rect.Left - obstacle.Right) <= MovementCollisionCalculator.MovementEpsilon;
}