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
    /// <summary>How far from the body's collider terrain counts as touching it.</summary>
    private const float ContactProbe = 0.02f;
    /// <summary>The probe along a side is shorter by this on both ends, so a wall does not count as the ceiling and vice versa.</summary>
    private const float ContactInset = 0.1f;

    private static readonly List<ICollider> ContactColliders = new();

    /// <summary>
    /// Per-object flying state.
    /// </summary>
    public static FlyingEnemyParameters FlyingParameters(this ISteeringCharacter obj) =>
        obj.GetParameters<FlyingEnemyParameters>();

    /// <summary>
    /// Accelerates the body towards the point (with arrival slow-down). Returns the distance to the point.
    /// Velocity pushing into touched terrain is removed (see <see cref="SlideAlongContacts"/>), so the body slides along
    /// a ceiling / wall instead of getting stuck on it.
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

        body.Velocity = SlideAlongContacts(body, MoveTowards(body.Velocity, desired, acceleration * dt));
        return distance;
    }

    /// <summary>
    /// Removes the velocity components pushing into static terrain the body touches (ceiling, floor, walls), so it
    /// moves only along it - e.g. purely horizontally under a ceiling instead of pressing up into it.
    /// </summary>
    public static Vector2 SlideAlongContacts(IBody body, Vector2 velocity)
    {
        if (body.Colliders is not { Length: > 0 })
            return velocity;

        var collider = body.Colliders[0];
        var aabb = collider.Aabb;
        var collidesWith = collider.Material.CollidesWith;

        if (velocity.Y < 0 && Touches(body, new Aabb(aabb.Left + ContactInset, aabb.Top - ContactProbe, aabb.Right - ContactInset, aabb.Top), collidesWith, ColliderSides.Bottom))
        {
            velocity.Y = 0;
        }
        else if (velocity.Y > 0 && Touches(body, new Aabb(aabb.Left + ContactInset, aabb.Bottom, aabb.Right - ContactInset, aabb.Bottom + ContactProbe), collidesWith, ColliderSides.Top))
        {
            velocity.Y = 0;
        }

        if (velocity.X < 0 && Touches(body, new Aabb(aabb.Left - ContactProbe, aabb.Top + ContactInset, aabb.Left, aabb.Bottom - ContactInset), collidesWith, ColliderSides.Right))
        {
            velocity.X = 0;
        }
        else if (velocity.X > 0 && Touches(body, new Aabb(aabb.Right, aabb.Top + ContactInset, aabb.Right + ContactProbe, aabb.Bottom - ContactInset), collidesWith, ColliderSides.Left))
        {
            velocity.X = 0;
        }

        return velocity;
    }

    /// <summary>True if the probe touches static terrain that blocks from the given side.</summary>
    private static bool Touches(IBody body, Aabb probe, ulong collidesWith, ColliderSides side)
    {
        ContactColliders.Clear();
        body.Simulation.CheckCollision(probe, body, collidesWith, 0, ContactColliders, ColliderType.Static);

        foreach (var contact in ContactColliders)
        {
            if (contact.Material.Sides.HasFlag(side))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Restarts the stuck check from the given position.
    /// </summary>
    public static void ResetStuck(this FlyingEnemyParameters parameters, Vector2 position)
    {
        parameters.StuckLastPosition = position;
        parameters.StuckProgress = 0;
        parameters.StuckTime = 0;
    }

    /// <summary>
    /// Stuck check: sums the movement towards <paramref name="goal"/> over windows of <paramref name="stuckTime"/> seconds.
    /// True at the end of a window in which the body got less than <paramref name="minProgress"/> closer - e.g. pressed
    /// against a wall / ceiling corner while steering straight at a point behind it (sliding back and forth along the
    /// ceiling with the sway does not count as progress). <paramref name="stuckTime"/> &lt;= 0 disables it.
    /// </summary>
    public static bool UpdateStuck(this FlyingEnemyParameters parameters, Vector2 position, Vector2 goal, float dt, float stuckTime, float minProgress = 0.5f)
    {
        if (stuckTime <= 0)
            return false;

        var toGoal = goal - parameters.StuckLastPosition;
        if (toGoal.LengthSquared() > 0.0001f)
        {
            parameters.StuckProgress += Vector2.Dot(position - parameters.StuckLastPosition, Vector2.Normalize(toGoal));
        }

        parameters.StuckLastPosition = position;
        parameters.StuckTime += dt;

        if (parameters.StuckTime < stuckTime)
            return false;

        var stuck = parameters.StuckProgress < minProgress;
        parameters.ResetStuck(position);
        return stuck;
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
