using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class ClimbBehavior(int climbMaterialIndex) : SteeringBehavior<ISteeringCharacter>()
{
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class Parameters
    {
        public Aabb? ClimbAabb;
    }

    protected override void OnEnter(ISteeringCharacter obj)
    {
        var parameters = obj.GetParameters<Parameters>(true);
        parameters.ClimbAabb = FindClimbAabb(obj);

        obj.Body.IsKinematic = true;
        obj.Body.Velocity = Vector2.Zero;

        if (parameters.ClimbAabb.HasValue)
        {
            var charAabb = obj.Body.Colliders[0].Aabb;
            var offsetX = obj.FaceLeft
                ? parameters.ClimbAabb.Value.Right - charAabb.Left
                : parameters.ClimbAabb.Value.Left - charAabb.Right;
            obj.Body.Position += new Vector2(offsetX, 0);
        }
    }

    protected override void OnExit(ISteeringCharacter obj)
    {
        obj.Body.IsKinematic = false;
        obj.SoundContainer?.StopLoop("Climb");
    }

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        if (obj.SteeringParameters.IsOnGround)
        {
            obj.SetSteeringState("Idle");
            return true;
        }
        
        var parameters = obj.GetParameters<Parameters>(true);
        parameters.ClimbAabb ??= FindClimbAabb(obj);

        if (parameters.ClimbAabb == null)
        {
            obj.SetSteeringState("Fall");
            return true;
        }

        var charAabb = obj.Body.Colliders[0].Aabb;
        var climbAabb = parameters.ClimbAabb.Value;

        // Once more than half the character is above the climbable surface, finish the climb
        // automatically as if Up were held, regardless of actual vertical input.
        var pastHalfway = charAabb.Top + charAabb.Height / 8 <= climbAabb.Top;
        var verticalMove = pastHalfway ? -1f : obj.SteeringInput.Value(SteeringControlNames.VerticalMove);

        // Reached the top while climbing up — shift onto it and keep moving in the facing direction
        if (verticalMove < 0 && charAabb.Bottom <= climbAabb.Top)
        {
            var direction = obj.FaceLeft ? -1f : 1f;
            obj.Body.Position += new Vector2(direction * 0.2f, 0);
            obj.Body.Velocity = new Vector2(direction * obj.PhysicsValues.WalkSpeed, 0);
            obj.SetSteeringState("Run");
            return true;
        }

        // Reached the bottom while climbing down
        if (verticalMove > 0 && charAabb.Top >= climbAabb.Bottom)
        {
            obj.SetSteeringState("Fall");
            return true;
        }

        if (verticalMove == 0)
        {
            obj.SoundContainer?.StopLoop("Climb");
            return false;
        }

        obj.SoundContainer?.PlayLoop("Climb");
        obj.Body.KinematicMove(new Vector2(0, verticalMove * obj.PhysicsValues.WallClimbSpeed * dt), KinematicMoveMode.Kinematic);
        return false;
    }

    private Aabb? FindClimbAabb(ISteeringCharacter obj)
    {
        var group = obj.Body.Colliders[0].Material.ColliderGroup;

        var aabb = obj.Body.Colliders[0].Aabb;
        var probe = obj.FaceLeft
            ? new Aabb(aabb.Left - 0.2f, aabb.Top, aabb.Left + 0.01f, aabb.Bottom)
            : new Aabb(aabb.Right - 0.01f, aabb.Top, aabb.Right + 0.2f, aabb.Bottom);

        var colliders = obj.Body.Simulation.GetColliders(probe, obj.Body, group);

        Aabb? climbAabb = null;
        foreach (var collider in colliders)
        {
            if (collider.Material.Index == climbMaterialIndex)
                climbAabb = climbAabb?.Union(collider.Aabb) ?? collider.Aabb;
        }
        return climbAabb;
    }
}
