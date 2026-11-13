using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Automatic climb up a wall made of the wallClimbMaterialIndex material.
/// The body is kinematic and moves up at WallClimbSpeed until it reaches the top (then steps onto it and runs);
/// it falls if the wall is lost or the movement is blocked.
/// </summary>
public class WallClimbBehavior(int wallClimbMaterialIndex) : SteeringBehavior<ISteeringCharacter>()
{
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class Parameters
    {
        // Combined bounds of the climbable wall beside the character
        public Aabb? ClimbAabb;
    }
    
    protected override void OnEnter(ISteeringCharacter obj)
    {
        var parameters = obj.GetParameters<Parameters>();
        parameters.ClimbAabb = FindClimbAabb(obj);
        
        // Take over movement: no gravity/physics while climbing
        obj.Body.IsKinematic = true;
        obj.Body.Velocity = Vector2.Zero;

        if (parameters.ClimbAabb.HasValue)
        {
            var charAabb = obj.Body.Colliders[0].Aabb;
            // Snap horizontally so the character is flush against the wall
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
        var parameters = obj.GetParameters<Parameters>();
        parameters.ClimbAabb ??= FindClimbAabb(obj);

        if (parameters.ClimbAabb == null)
        {
            obj.SetSteeringState("Fall");
            return true;
        }

        // Start the looped climbing sound (repeated call is expected to be harmless)
        obj.SoundContainer?.PlayLoop("Climb");
        
        var charAabb = obj.Body.Colliders[0].Aabb;
        var climbAabb = parameters.ClimbAabb.Value;
        
        // Reached the top — shift forward to stand on it
        if (charAabb.Bottom <= climbAabb.Top)
        {
            var shift = obj.FaceLeft ? -0.6f : 0.6f;
            obj.Body.Position += new Vector2(shift, 0);
            obj.Body.Velocity = Vector2.Zero;
            obj.SetSteeringState("Run");
            return true;
        }

        var previousPosition = obj.Body.Position;
        obj.Body.KinematicMove(new Vector2(0, -obj.PhysicsValues.WallClimbSpeed * dt), KinematicMoveMode.Kinematic);
        // Blocked (e.g. ceiling) - the body did not move up, so let go
        if (obj.Body.Position.Y >= previousPosition.Y)
        {
            obj.SetSteeringState("Fall");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Looks for wall-climb colliders in a thin strip on the facing side and returns their union (null if none).
    /// The probe ends slightly above the feet so the floor is not detected.
    /// </summary>
    private Aabb? FindClimbAabb(ISteeringCharacter obj)
    {
        var collidesWith = obj.Body.Colliders[0].Material.CollidesWith;

        var aabb = obj.Body.Colliders[0].Aabb;
        var probe = obj.FaceLeft
            ? new Aabb(aabb.Left - 0.2f, aabb.Top, aabb.Left + 0.01f, aabb.Bottom - 0.05f)
            : new Aabb(aabb.Right - 0.01f, aabb.Top, aabb.Right + 0.2f, aabb.Bottom - 0.05f);

        var colliders = obj.Body.Simulation.GetColliders(probe, obj.Body, collidesWith);

        Aabb? climbAabb = null;
        foreach (var collider in colliders)
        {
            if (collider.Material.Index == wallClimbMaterialIndex)
                climbAabb = climbAabb?.Union(collider.Aabb) ?? collider.Aabb;
        }
        return climbAabb;
    }
}
