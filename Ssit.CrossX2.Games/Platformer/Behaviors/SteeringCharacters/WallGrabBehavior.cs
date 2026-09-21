using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class WallGrabBehavior(int grabMaterialIndex) : SteeringBehavior<ISteeringCharacter>
{
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class Parameters
    {
        public Vector2 TargetPosition;
    }

    protected override void OnEnter(ISteeringCharacter obj)
    {
        obj.Body.IsKinematic = true;
        obj.Body.Velocity = Vector2.Zero;

        var grabAabb = FindGrabAabb(obj);
        if (grabAabb.HasValue)
        {
            var charAabb = obj.Body.Colliders[0].Aabb;
            var offsetX = obj.FaceLeft
                ? grabAabb.Value.Center.X - charAabb.Left
                : grabAabb.Value.Center.X - charAabb.Right;
            var offsetY = grabAabb.Value.Center.Y - charAabb.Center.Y;
            obj.GetParameters<Parameters>(true).TargetPosition = obj.Body.Position + new Vector2(offsetX, offsetY);
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

        var targetPosition = obj.GetParameters<Parameters>(true).TargetPosition;
        obj.Body.Position = Vector2.Lerp(obj.Body.Position, targetPosition, MathF.Min(1f, 12f * dt));
        return false;
    }

    private Aabb? FindGrabAabb(ISteeringCharacter obj)
    {
        var group = obj.Body.Colliders[0].Material.ColliderGroup;
        
        var aabb = obj.Body.Colliders[0].Aabb;
        var probe = obj.FaceLeft
            ? new Aabb(aabb.Left - 0.2f, aabb.Top, aabb.Left + 0.01f, aabb.Bottom)
            : new Aabb(aabb.Right - 0.01f, aabb.Top, aabb.Right + 0.2f, aabb.Bottom);

        var colliders = obj.Body.Simulation.GetColliders(probe, obj.Body, group, colliderType: ColliderType.Trigger);

        Aabb? grabAabb = null;
        foreach (var collider in colliders)
        {
            if (collider.Material.Index == grabMaterialIndex)
                grabAabb = grabAabb?.Union(collider.Aabb) ?? collider.Aabb;
        }
        return grabAabb;
    }
}
