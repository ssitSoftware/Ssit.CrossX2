using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class WallGrabCheckBehavior(int grabMaterialIndex) : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var charAabb = obj.Body.Colliders[0].Aabb;
        var probe = obj.FaceLeft
            ? new Aabb(charAabb.Left - 0.2f, charAabb.Top, charAabb.Left + 0.01f, charAabb.Bottom)
            : new Aabb(charAabb.Right - 0.01f, charAabb.Top, charAabb.Right + 0.2f, charAabb.Bottom);

        var group = obj.Body.Colliders[0].Material.ColliderGroup;
        
        var colliders = obj.Body.Simulation.GetColliders(probe, obj.Body, group, colliderType: ColliderType.Trigger);
        foreach (var collider in colliders)
        {
            if (collider.Material.Index != grabMaterialIndex)
                continue;

            var grabAabb = collider.Aabb;
            if (charAabb.Top + 0.1f < grabAabb.Top && charAabb.Bottom - 0.1f > grabAabb.Bottom)
            {
                obj.SetSteeringState("WallGrab");
                return true;
            }
        }

        return false;
    }
}
