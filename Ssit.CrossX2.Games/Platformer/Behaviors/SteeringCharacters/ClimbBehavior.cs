using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Climbing on a ladder-like surface (collider material climbMaterialIndex) next to the character.
/// The body is kinematic while climbing and moves up/down with vertical input. Exits: reaching the top (steps onto it and runs),
/// reaching the bottom or losing the surface (falls), touching the ground (idle).
/// </summary>
public class ClimbBehavior(int climbMaterialIndex) : SteeringBehavior<ISteeringCharacter>()
{
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class Parameters
    {
        // Combined bounds of the climbable surface beside the character (null if none was found)
        public Aabb? ClimbAabb;
    }

    protected override void OnEnter(ISteeringCharacter obj)
    {
        var parameters = ((ICharacter)obj).GetParameters<Parameters>();
        parameters.ClimbAabb = FindClimbAabb(obj);

        // Take over movement: no gravity/physics while climbing, we move the body ourselves
        obj.Body.IsKinematic = true;
        obj.Body.Velocity = Vector2.Zero;

        if (parameters.ClimbAabb.HasValue)
        {
            var charAabb = obj.Body.Colliders[0].Aabb;
            // Snap horizontally so the character's side is flush against the climbable surface
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
        
        var parameters = ((ICharacter)obj).GetParameters<Parameters>();
        // Retry the lookup in case the surface was not found on enter
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

        // Hanging still: stop the climbing sound, stay in place
        if (verticalMove == 0)
        {
            obj.SoundContainer?.StopLoop("Climb");
            return false;
        }

        obj.SoundContainer?.PlayLoop("Climb");
        // Move along the surface (negative Y is up)
        obj.Body.KinematicMove(new Vector2(0, verticalMove * obj.PhysicsValues.WallClimbSpeed * dt), KinematicMoveMode.Kinematic);
        return false;
    }

    /// <summary>
    /// Looks for climbable colliders in a thin strip on the side the character faces and returns their union (null if none).
    /// </summary>
    private Aabb? FindClimbAabb(ISteeringCharacter obj)
    {
        var collidesWith = obj.Body.Colliders[0].Material.CollidesWith;

        var aabb = obj.Body.Colliders[0].Aabb;
        var probe = obj.FaceLeft
            ? new Aabb(aabb.Left - 0.2f, aabb.Top, aabb.Left + 0.01f, aabb.Bottom)
            : new Aabb(aabb.Right - 0.01f, aabb.Top, aabb.Right + 0.2f, aabb.Bottom);

        var colliders = obj.Body.Simulation.GetColliders(probe, obj.Body, collidesWith);

        Aabb? climbAabb = null;
        foreach (var collider in colliders)
        {
            if (collider.Material.Index == climbMaterialIndex)
                climbAabb = climbAabb?.Union(collider.Aabb) ?? collider.Aabb;
        }
        return climbAabb;
    }
}
