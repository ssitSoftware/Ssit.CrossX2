using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class MoveHorizontalBehavior() : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        if (!obj.SteeringParameters.IsOnGround)
        {
            return false;
        }

        var move = obj.SteeringInput.Value(SteeringControlNames.HorizontalMove);

        if (MathF.Abs(move) > 0.1f)
        {
            var physicsValues = obj.PhysicsValues;
            var velocity = obj.Body.Velocity;
            var factor = MathF.Max(0.01f, MathF.Min(1, obj.SteeringParameters.GroundMaterial.Friction * obj.Body.Colliders[0].Material.Friction));

            var newX = Math.Clamp(velocity.X + move * physicsValues.Acceleration * dt * factor, -physicsValues.RunSpeed, physicsValues.RunSpeed);

            obj.Body.Velocity = velocity with { X = newX };
            obj.FaceLeft = move < 0;

            if (IsBlockedAhead(obj, move < 0))
                return false;

            obj.SetSteeringState(MathF.Abs(newX) < physicsValues.WalkSpeed ? "Walk" : "Run");
            obj.SteeringParameters.LastHorizontalVelocity = obj.Body.Velocity.X;
        }
        else
        {
            obj.SetSteeringState("Idle");
        }
        return false;
    }

    private static bool IsBlockedAhead(ISteeringCharacter obj, bool moveLeft)
    {
        var charAabb = obj.Body.Colliders[0].Aabb;
        var bottom = charAabb.Bottom - (obj.Body.StepUpTolerance + float.Epsilon);

        var probe = moveLeft
            ? new Aabb(charAabb.Left - 0.2f, charAabb.Top, charAabb.Left + 0.01f, bottom)
            : new Aabb(charAabb.Right - 0.01f, charAabb.Top, charAabb.Right + 0.2f, bottom);

        var group = obj.Body.Colliders[0].Material.ColliderGroup;
        var colliders = obj.Body.Simulation.GetColliders(probe, obj.Body, group);
        return colliders.Count > 0;
    }
}