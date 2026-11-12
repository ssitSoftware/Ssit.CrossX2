using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Mid-air jump that is only possible while the character's feet overlap a trigger implementing <see cref="IAirJumpPad"/>
/// (e.g. a bounce pad or floating orb). Consumes the Jump press and launches the character.
/// </summary>
public class AirJumpBehavior : SteeringBehavior<ISteeringCharacter>
{
    /// <summary>
    /// Implemented by trigger objects that can launch a character in mid-air.
    /// </summary>
    public interface IAirJumpPad
    {
        // Returns true if the pad fired. direction, when set, overrides the launch velocity; otherwise the default jump velocity is used.
        bool Activate(out Vector2? direction);
    }

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        if (obj.SteeringParameters.IsOnGround)
            return false;
        
        if (obj.SteeringInput.Button(SteeringControlNames.Jump) != ButtonState.JustPressed)
            return false;

        var collidesWith = obj.Body.Colliders[0].Material.CollidesWith;
        
        var charAabb = obj.Body.Colliders[0].Aabb;
        // Small box around the feet, slightly inset horizontally, used to find pads the character is touching
        var feetProbe = new Aabb(
            charAabb.Left + 0.1f,
            charAabb.Bottom - 0.2f,
            charAabb.Right - 0.1f,
            charAabb.Bottom + 0.1f);

        var colliders = obj.Body.Simulation.GetColliders(feetProbe, obj.Body, collidesWith, colliderType: ColliderType.Trigger);
        foreach (var collider in colliders)
        {
            if (collider?.AttachedBody?.Owner is not IAirJumpPad jp)
                continue;
            
            if (jp.Activate(out var dir))
            {
                obj.SetSteeringState("Raise");

                // Snap to the pad's center so the launch always starts from the same spot
                obj.Body.Position = collider.Aabb.Center;
                
                // Default launch: standard jump height and run speed in the facing direction (Y grows downward, hence the minus)
                obj.Body.Velocity = obj.Body.Velocity with { Y = -obj.PhysicsValues.JumpVelocity };
                obj.Body.Velocity = obj.Body.Velocity with { X = obj.FaceLeft ? -obj.PhysicsValues.RunSpeed : obj.PhysicsValues.RunSpeed };

                // A pad-specific direction replaces the default launch velocity and may turn the character
                if (dir.HasValue)
                {
                    obj.Body.Velocity = dir.Value;
                    if (dir.Value.X != 0)
                    {
                        obj.FaceLeft = dir.Value.X < 0;
                    }
                }

                obj.SteeringParameters.LastHorizontalVelocity = obj.Body.Velocity.X;

                return true;
            }
        }

        return false;
    }
}
