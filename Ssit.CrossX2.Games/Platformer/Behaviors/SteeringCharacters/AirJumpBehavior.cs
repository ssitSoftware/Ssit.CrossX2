using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class AirJumpBehavior : SteeringBehavior<ISteeringCharacter>
{
    public interface IAirJumpPad
    {
        bool Activate(out Vector2? direction);
    }

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        if (obj.SteeringParameters.IsOnGround)
            return false;
        
        if (obj.SteeringInput.Button(SteeringControlNames.Jump) != ButtonState.JustPressed)
            return false;

        var group = obj.Body.Colliders[0].Material.ColliderGroup;
        
        var charAabb = obj.Body.Colliders[0].Aabb;
        var feetProbe = new Aabb(
            charAabb.Left + 0.1f,
            charAabb.Bottom - 0.2f,
            charAabb.Right - 0.1f,
            charAabb.Bottom + 0.1f);

        var colliders = obj.Body.Simulation.GetColliders(feetProbe, obj.Body, group, colliderType: ColliderType.Trigger);
        foreach (var collider in colliders)
        {
            if (collider?.AttachedBody?.Owner is not IAirJumpPad jp)
                continue;
            
            if (jp.Activate(out var dir))
            {
                obj.SetSteeringState("Raise");

                obj.Body.Position = collider.Aabb.Center;
                
                obj.Body.Velocity = obj.Body.Velocity with { Y = -obj.PhysicsValues.JumpVelocity };
                obj.Body.Velocity = obj.Body.Velocity with { X = obj.FaceLeft ? -obj.PhysicsValues.RunSpeed : obj.PhysicsValues.RunSpeed };

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
