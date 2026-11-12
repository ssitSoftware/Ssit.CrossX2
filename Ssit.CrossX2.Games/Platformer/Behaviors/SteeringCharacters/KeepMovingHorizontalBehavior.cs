using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Applies horizontal input on the ground like <see cref="MoveHorizontalBehavior"/>, but never changes the steering state.
/// Use in ground states that should keep momentum (e.g. a jump wind-up) so the following jump starts with full horizontal speed.
/// </summary>
public class KeepMovingHorizontalBehavior : SingleSteeringBehavior<ISteeringCharacter, KeepMovingHorizontalBehavior>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        if (!obj.SteeringParameters.IsOnGround)
            return false;

        var move = obj.SteeringInput.Value(SteeringControlNames.HorizontalMove);
        if (MathF.Abs(move) <= 0.1f)
            return false;

        var physicsValues = obj.PhysicsValues;
        var velocity = obj.Body.Velocity;
        var factor = MathF.Max(0.01f, MathF.Min(1, obj.SteeringParameters.GroundMaterial.Friction * obj.Body.Colliders[0].Material.Friction));

        var walk = obj.SteeringInput.Button(SteeringControlNames.Walk).IsDown;
        var maxSpeed = walk ? physicsValues.WalkSpeed : physicsValues.RunSpeed;

        var newX = Math.Clamp(velocity.X + move * physicsValues.Acceleration * dt * factor, -maxSpeed, maxSpeed);

        obj.Body.Velocity = velocity with { X = newX };
        obj.FaceLeft = move < 0;
        obj.SteeringParameters.LastHorizontalVelocity = newX;
        return false;
    }
}
