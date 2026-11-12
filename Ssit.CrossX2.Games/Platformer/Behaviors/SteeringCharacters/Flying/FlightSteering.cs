using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Flying;

/// <summary>
/// Shared helpers for the flying enemy behaviors.
/// </summary>
internal static class FlightSteering
{
    /// <summary>
    /// Per-object flying state.
    /// </summary>
    public static FlyingEnemyParameters FlyingParameters(this ISteeringCharacter obj) =>
        ((ICharacter)obj).GetParameters<FlyingEnemyParameters>();

    /// <summary>
    /// Accelerates the body towards the point (with arrival slow-down). Returns the distance to the point.
    /// </summary>
    public static float SteerTowards(IBody body, Vector2 point, float maxSpeed, float acceleration, float slowRadius, float dt)
    {
        var toPoint = point - body.Position;
        var distance = toPoint.Length();

        var desired = Vector2.Zero;
        if (distance > 0.001f)
        {
            // Inside slowRadius the desired speed shrinks linearly so the body arrives without overshooting
            var speed = slowRadius > 0 && distance < slowRadius ? maxSpeed * distance / slowRadius : maxSpeed;
            desired = toPoint / distance * speed;
        }

        body.Velocity = MoveTowards(body.Velocity, desired, acceleration * dt);
        return distance;
    }

    /// <summary>
    /// Moves current toward target by at most maxDelta (vector length), snapping to target when close enough.
    /// </summary>
    public static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxDelta)
    {
        var diff = target - current;
        var length = diff.Length();

        if (length <= maxDelta || length < 0.0001f)
            return target;

        return current + diff / length * maxDelta;
    }

    /// <summary>
    /// Hit filter: accepts only hittables hostile to the owner of the body.
    /// </summary>
    public static bool IsEnemyOf(IBody body, IHittable hittable) =>
        (body.Owner as IHittable)?.IsEnemy(hittable as ISteeringCharacter) ?? false;
}
