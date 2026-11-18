using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Flying;

/// <summary>
/// Flies to a point above the target and sways side to side there. After hovering for <see cref="HoverTime"/>
/// switches to <paramref name="attackState"/>. When the target is dead or too far, switches to <paramref name="loseState"/>.
/// Also gives up (switches to <paramref name="loseState"/>) when stuck on terrain for <see cref="StuckTime"/> - the target
/// is behind a wall and there is no straight way to it. Then the target is not reacquired for <see cref="ReacquireCooldown"/>.
/// </summary>
public class FlyOverTargetBehavior(string attackState, string loseState) : SteeringBehavior<ISteeringCharacter>
{
    /// <summary>Hover point relative to the target position (target position is at its feet).</summary>
    public Vector2 HoverOffset { get; init; } = new(0, -5f);
    public float Speed { get; init; } = 8f;
    public float Acceleration { get; init; } = 24f;
    public float SlowRadius { get; init; } = 2f;
    public float SwayAmplitude { get; init; } = 2f;
    public float SwayFrequency { get; init; } = 0.35f;
    /// <summary>Distance from the hover point that counts as "hovering over the target".</summary>
    public float HoverRadius { get; init; } = 1.5f;
    /// <summary>How long to hover over the target before attacking.</summary>
    public float HoverTime { get; init; } = 1.5f;
    public float LoseRange { get; set; } = 16f;
    /// <summary>How long the body can be blocked (not hovering, not moving) before giving up. 0 = never.</summary>
    public float StuckTime { get; init; } = 1.5f;
    /// <summary>After giving up on an unreachable target - time before it can be reacquired.</summary>
    public float ReacquireCooldown { get; init; } = 2f;

    protected override void OnEnter(ISteeringCharacter obj)
    {
        var parameters = obj.FlyingParameters();
        parameters.HoverTime = 0;
        parameters.ResetStuck(obj.Body.Position);
    }

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var parameters = obj.FlyingParameters();
        var target = parameters.Target;
        var position = obj.Body.Position;

        if (target is not { Alive: true } || Vector2.Distance(target.Position, position) > LoseRange)
        {
            return Lose(obj, parameters, 0);
        }

        parameters.FlightTime += dt;

        // Horizontal side-to-side sway around the point above the target
        var sway = MathF.Sin(parameters.FlightTime * SwayFrequency * MathF.Tau) * SwayAmplitude;
        var basePoint = target.Position + HoverOffset;
        var hoverPoint = basePoint + new Vector2(sway, 0);

        var distance = FlightSteering.SteerTowards(obj.Body, hoverPoint, Speed, Acceleration, SlowRadius, dt);
        obj.FaceLeft = target.Position.X < position.X;

        // Only time spent close to the hover point counts toward the attack
        if (distance < HoverRadius)
        {
            parameters.HoverTime += dt;
        }

        // Close to the sway area over the target - not stuck. Otherwise check for being pressed against terrain
        // (the hover point is behind a wall / ceiling - no straight way to it).
        if (Vector2.Distance(basePoint, position) < HoverRadius + SwayAmplitude)
        {
            parameters.ResetStuck(position);
        }
        else if (parameters.UpdateStuck(position, basePoint, dt, StuckTime))
        {
            return Lose(obj, parameters, ReacquireCooldown);
        }

        if (parameters.HoverTime < HoverTime)
            return false;

        obj.SetSteeringState(attackState);
        return true;
    }

    private bool Lose(ISteeringCharacter obj, FlyingEnemyParameters parameters, float reacquireCooldown)
    {
        // Remembered - it can be reacquired when it comes back near home (see DetectTargetInRangeBehavior.NearHome).
        parameters.LastTarget = parameters.Target ?? parameters.LastTarget;
        parameters.Target = null;
        parameters.ReacquireCooldown = reacquireCooldown;
        obj.SetSteeringState(loseState);
        return true;
    }
}
