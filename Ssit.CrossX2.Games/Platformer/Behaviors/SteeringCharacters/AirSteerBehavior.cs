using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class AirSteerBehavior : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var move = obj.SteeringInput.Value(SteeringControlNames.HorizontalMove);

        if (MathF.Abs(move) <= 0.1f)
            return false;

        var physicsValues = obj.PhysicsValues;
        var maxAirSpeed = MathF.Max(MathF.Abs(obj.SteeringParameters.LastHorizontalVelocity), physicsValues.AirControlZeroSpeed);

        if (MathF.Sign(move) != MathF.Sign(obj.SteeringParameters.LastHorizontalVelocity))
        {
            maxAirSpeed = physicsValues.AirControlZeroSpeed / 2;
        }
        
        var target = move * maxAirSpeed;
        var maxDelta = physicsValues.AirAcceleration * dt;
        var velocity = obj.Body.Velocity;

        var newX = MathF.Abs(target - velocity.X) <= maxDelta
            ? target
            : velocity.X + MathF.Sign(target - velocity.X) * maxDelta;

        obj.Body.Velocity = velocity with { X = newX };
        obj.FaceLeft = move < 0;

        return false;
    }
}
