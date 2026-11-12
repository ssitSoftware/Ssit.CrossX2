using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Air control: accelerates the horizontal velocity toward the input direction while airborne.
/// Speed is capped by the momentum the character had when leaving the ground (but never below AirControlZeroSpeed),
/// so a standing jump can still be steered a little. Never changes the steering state.
/// </summary>
public class AirSteerBehavior : SingleSteeringBehavior<ISteeringCharacter, AirSteerBehavior>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var move = obj.SteeringInput.Value(SteeringControlNames.HorizontalMove);

        // Dead zone - without input the current momentum is kept untouched
        if (MathF.Abs(move) <= 0.1f)
            return false;

        var physicsValues = obj.PhysicsValues;
        var maxAirSpeed = MathF.Max(MathF.Abs(obj.SteeringParameters.LastHorizontalVelocity), physicsValues.AirControlZeroSpeed);

        // Steering against the jump direction is only allowed at a reduced speed
        if (MathF.Sign(move) != MathF.Sign(obj.SteeringParameters.LastHorizontalVelocity))
        {
            maxAirSpeed = physicsValues.AirControlZeroSpeed / 2;
        }
        
        var target = move * maxAirSpeed;
        var maxDelta = physicsValues.AirAcceleration * dt;
        var velocity = obj.Body.Velocity;

        // Move toward the target speed by at most AirAcceleration * dt, without overshooting
        var newX = MathF.Abs(target - velocity.X) <= maxDelta
            ? target
            : velocity.X + MathF.Sign(target - velocity.X) * maxDelta;

        obj.Body.Velocity = velocity with { X = newX };
        obj.FaceLeft = move < 0;

        return false;
    }
}
