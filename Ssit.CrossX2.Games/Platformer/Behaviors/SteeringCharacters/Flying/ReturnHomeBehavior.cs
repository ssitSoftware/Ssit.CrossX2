using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Flying;

/// <summary>Flies back to <see cref="FlyingEnemyParameters.HomePosition"/> and switches to <paramref name="landState"/>.</summary>
public class ReturnHomeBehavior(string landState) : SteeringBehavior<ISteeringCharacter>
{
    public float Speed { get; init; } = 6f;
    public float Acceleration { get; init; } = 16f;
    public float SlowRadius { get; init; } = 1.5f;
    public float ArriveDistance { get; init; } = 0.15f;

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var home = obj.FlyingParameters().HomePosition;
        var distance = FlightSteering.SteerTowards(obj.Body, home, Speed, Acceleration, SlowRadius, dt);

        var velocity = obj.Body.Velocity;
        if (MathF.Abs(velocity.X) > 0.1f)
        {
            obj.FaceLeft = velocity.X < 0;
        }

        if (distance > ArriveDistance)
            return false;

        obj.Body.Position = home;
        obj.Body.Velocity = Vector2.Zero;
        obj.SetSteeringState(landState);
        return true;
    }
}
