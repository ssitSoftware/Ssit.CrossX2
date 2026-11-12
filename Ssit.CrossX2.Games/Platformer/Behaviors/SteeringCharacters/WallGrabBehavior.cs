using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Hanging on a grab point (trigger with material grabMaterialIndex). The body is kinematic and smoothly pulled
/// to the grab point's center; the character falls if the grab area disappears.
/// </summary>
public class WallGrabBehavior(int grabMaterialIndex) : SteeringBehavior<ISteeringCharacter>
{
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class Parameters
    {
        // Position at which the character hangs from the grab point
        public Vector2 TargetPosition;
    }

    protected override void OnEnter(ISteeringCharacter obj)
    {
        // No physics while hanging
        obj.Body.IsKinematic = true;
        obj.Body.Velocity = Vector2.Zero;

        var grabAabb = FindGrabAabb(obj);
        if (grabAabb.HasValue)
        {
            var charAabb = obj.Body.Colliders[0].Aabb;
            // Compute where the character's facing side must be to align with the grab point's center
            var offsetX = obj.FaceLeft
                ? grabAabb.Value.Center.X - charAabb.Left
                : grabAabb.Value.Center.X - charAabb.Right;
            var offsetY = grabAabb.Value.Center.Y - charAabb.Center.Y;
            ((ICharacter)obj).GetParameters<Parameters>().TargetPosition = obj.Body.Position + new Vector2(offsetX, offsetY);
            obj.SoundContainer?.Play("WallGrab");
        }
    }

    protected override void OnExit(ISteeringCharacter obj)
    {
        obj.Body.IsKinematic = false;
    }

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        if (FindGrabAabb(obj) == null)
        {
            obj.SetSteeringState("Fall");
            return true;
        }

        var targetPosition = ((ICharacter)obj).GetParameters<Parameters>().TargetPosition;
        // Exponential-style smoothing toward the hang position, frame-rate independent via dt
        obj.Body.Position = Vector2.Lerp(obj.Body.Position, targetPosition, MathF.Min(1f, 12f * dt));
        return false;
    }

    /// <summary>
    /// Finds grab-point triggers in a thin strip on the facing side and returns their union (null if none).
    /// </summary>
    private Aabb? FindGrabAabb(ISteeringCharacter obj)
    {
        var collidesWith = obj.Body.Colliders[0].Material.CollidesWith;

        var aabb = obj.Body.Colliders[0].Aabb;
        var probe = obj.FaceLeft
            ? new Aabb(aabb.Left - 0.2f, aabb.Top, aabb.Left + 0.01f, aabb.Bottom)
            : new Aabb(aabb.Right - 0.01f, aabb.Top, aabb.Right + 0.2f, aabb.Bottom);

        var colliders = obj.Body.Simulation.GetColliders(probe, obj.Body, collidesWith, colliderType: ColliderType.Trigger);

        Aabb? grabAabb = null;
        foreach (var collider in colliders)
        {
            if (collider.Material.Index == grabMaterialIndex)
                grabAabb = grabAabb?.Union(collider.Aabb) ?? collider.Aabb;
        }
        return grabAabb;
    }
}
