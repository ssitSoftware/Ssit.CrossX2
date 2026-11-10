using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Utils;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Flying;

/// <summary>
/// Dives in a straight line towards the target (direction locked on enter). Hits the target on contact.
/// Ends (switching to <paramref name="afterState"/>) on hit, when blocked by terrain, after overshooting the target
/// or after <see cref="MaxTime"/>.
/// </summary>
public class DiveAttackBehavior(BoxHitDetector hitBox, string afterState) : SteeringBehavior<ISteeringCharacter>
{
    public float Speed { get; init; } = 16f;
    public float Power { get; init; } = 8f;
    public float MaxTime { get; init; } = 1.2f;
    /// <summary>Aim point relative to target position (target position is at its feet).</summary>
    public Vector2 AimOffset { get; init; } = new(0, -1f);
    /// <summary>How far past the aim point the dive continues.</summary>
    public float Overshoot { get; init; } = 1.5f;

    protected override void OnEnter(ISteeringCharacter obj)
    {
        var parameters = obj.FlyingParameters();
        parameters.StateTime = 0;

        var direction = new Vector2(obj.FaceLeft ? -0.5f : 0.5f, 1f);
        if (parameters.Target is { } target)
        {
            direction = target.Position + AimOffset - obj.Body.Position;
        }

        direction = direction.LengthSquared() > 0.0001f ? Vector2.Normalize(direction) : Vector2.UnitY;

        parameters.AttackDirection = direction;
        obj.FaceLeft = direction.X < 0;
        obj.Body.Velocity = direction * Speed;
    }

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var parameters = obj.FlyingParameters();
        var direction = parameters.AttackDirection;
        parameters.StateTime += dt;

        var hit = hitBox.Detect(obj.Body, obj.FaceLeft, IMaterial.AllColliders, FlightSteering.IsEnemyOf, ColliderSides.None);
        if (hit.Hittable is { Alive: true } hittable)
        {
            hittable.Hit(direction, Power, obj as IHittable);
            return Finish(obj);
        }

        // Physics zeroes velocity on the blocked axis - terrain stopped the dive.
        var blocked = parameters.StateTime > 0.05f && Vector2.Dot(obj.Body.Velocity, direction) < Speed * 0.5f;

        var overshot = parameters.Target is { } target &&
                       Vector2.Dot(target.Position + AimOffset - obj.Body.Position, direction) < -Overshoot;

        if (blocked || overshot || parameters.StateTime > MaxTime)
            return Finish(obj);

        obj.Body.Velocity = direction * Speed;
        return false;
    }

    private bool Finish(ISteeringCharacter obj)
    {
        obj.SetSteeringState(afterState);
        return true;
    }
}
