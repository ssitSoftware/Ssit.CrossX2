using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Flying;

/// <summary>
/// Flies back to <see cref="FlyingEnemyParameters.HomePosition"/> and switches to <paramref name="landState"/>.
/// There is no path finding - it flies straight home. When blocked by terrain for <see cref="StuckTime"/>:
/// - if neither the object nor home is visible (<see cref="Camera"/>), it is moved straight home,
/// - otherwise it makes a detour (sideways and a bit back, alternating sides, longer each time) and tries again.
/// </summary>
public class ReturnHomeBehavior(string landState) : SteeringBehavior<ISteeringCharacter>
{
    public float Speed { get; init; } = 6f;
    public float Acceleration { get; init; } = 16f;
    public float SlowRadius { get; init; } = 1.5f;
    public float ArriveDistance { get; init; } = 0.15f;
    /// <summary>How long the body can be blocked (not moving) before a detour / moving home. 0 = never.</summary>
    public float StuckTime { get; init; } = 0.75f;
    /// <summary>Duration of the first detour (later ones are longer).</summary>
    public float DetourTime { get; init; } = 0.6f;
    /// <summary>Camera used to move the object home when stuck out of sight. Null = never moved.</summary>
    public ICamera Camera { get; init; }
    /// <summary>Half size of the area around the object / home that must be out of sight to move it home.</summary>
    public float OutOfSightMargin { get; init; } = 2f;

    protected override void OnEnter(ISteeringCharacter obj)
    {
        var parameters = obj.FlyingParameters();
        parameters.ResetStuck(obj.Body.Position);
        parameters.DetourTime = 0;
        parameters.DetourCount = 0;
    }

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var parameters = obj.FlyingParameters();
        var home = parameters.HomePosition;
        var position = obj.Body.Position;
        float distance;

        if (parameters.DetourTime > 0)
        {
            parameters.DetourTime -= dt;
            FlightSteering.SteerTowards(obj.Body, position + parameters.DetourDirection * Speed, Speed, Acceleration, 0, dt);
            distance = Vector2.Distance(home, position);
        }
        else
        {
            distance = FlightSteering.SteerTowards(obj.Body, home, Speed, Acceleration, SlowRadius, dt);
        }

        var velocity = obj.Body.Velocity;
        // Face the flight direction (ignore near-vertical flight to avoid flicker)
        if (MathF.Abs(velocity.X) > 0.1f)
        {
            obj.FaceLeft = velocity.X < 0;
        }

        if (distance <= ArriveDistance)
            return Land(obj, home);

        // Slowing down to arrive - not stuck
        if (parameters.DetourTime <= 0 && distance < SlowRadius)
        {
            parameters.ResetStuck(position);
            return false;
        }

        var goal = parameters.DetourTime > 0 ? position + parameters.DetourDirection : home;
        if (!parameters.UpdateStuck(position, goal, dt, StuckTime))
            return false;

        // Blocked by terrain - there is no straight way home from here
        if (IsOutOfSight(position) && IsOutOfSight(home))
            return Land(obj, home);

        StartDetour(parameters, home - position);
        return false;
    }

    private void StartDetour(FlyingEnemyParameters parameters, Vector2 toHome)
    {
        var direction = toHome.LengthSquared() > 0.0001f ? Vector2.Normalize(toHome) : -Vector2.UnitY;

        // Sideways (alternating sides) and a bit back - away from the obstacle in the way
        var side = parameters.DetourCount % 2 == 0 ? 1f : -1f;
        var sideways = new Vector2(-direction.Y, direction.X) * side;

        parameters.DetourDirection = Vector2.Normalize(sideways - direction * 0.5f);
        parameters.DetourTime = DetourTime * (1 + parameters.DetourCount / 2);
        parameters.DetourCount++;
    }

    private bool IsOutOfSight(Vector2 position)
    {
        var visibleBounds = Camera?.VisibleBounds ?? default;
        if (visibleBounds.IsEmpty)
            return false;

        var area = new RectangleF(position.X - OutOfSightMargin, position.Y - OutOfSightMargin, OutOfSightMargin * 2, OutOfSightMargin * 2);
        return !visibleBounds.IsIntersecting(area);
    }

    private bool Land(ISteeringCharacter obj, Vector2 home)
    {
        // Arrived (or moved home out of sight): snap exactly onto the perch and stop
        obj.Body.Position = home;
        obj.Body.Velocity = Vector2.Zero;
        obj.SetSteeringState(landState);
        return true;
    }
}
