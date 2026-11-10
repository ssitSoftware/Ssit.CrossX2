using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Flying;

/// <summary>
/// Flies to a point above the target and sways side to side there. After hovering for <see cref="HoverTime"/>
/// switches to <paramref name="attackState"/>. When the target is dead or too far, switches to <paramref name="loseState"/>.
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

    protected override void OnEnter(ISteeringCharacter obj)
    {
        obj.FlyingParameters().HoverTime = 0;
    }

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var parameters = obj.FlyingParameters();
        var target = parameters.Target;
        var position = obj.Body.Position;

        if (target is not { Alive: true } || Vector2.Distance(target.Position, position) > LoseRange)
        {
            parameters.Target = null;
            obj.SetSteeringState(loseState);
            return true;
        }

        parameters.FlightTime += dt;

        var sway = MathF.Sin(parameters.FlightTime * SwayFrequency * MathF.Tau) * SwayAmplitude;
        var hoverPoint = target.Position + HoverOffset + new Vector2(sway, 0);

        var distance = FlightSteering.SteerTowards(obj.Body, hoverPoint, Speed, Acceleration, SlowRadius, dt);
        obj.FaceLeft = target.Position.X < position.X;

        if (distance < HoverRadius)
        {
            parameters.HoverTime += dt;
        }

        if (parameters.HoverTime < HoverTime)
            return false;

        obj.SetSteeringState(attackState);
        return true;
    }
}
