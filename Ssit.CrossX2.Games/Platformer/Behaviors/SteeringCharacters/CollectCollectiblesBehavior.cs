using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class CollectCollectiblesBehavior<TObject> : SteeringBehavior<ISteeringCharacter> where TObject: ISteeringCharacter, ICollector
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var group = obj.Body.Colliders[0].Material.ColliderGroup;
        
        var charAabb = obj.Body.Colliders[0].Aabb;
        var colliders = obj.Body.Simulation.GetColliders(charAabb, obj.Body, group, colliderType: ColliderType.Trigger);

        foreach (var collider in colliders)
        {
            if (!collider.IsActive)
                continue;
            
            if (collider.AttachedBody?.Owner is ICollectible collectible)
            {
                ((ICollector)obj).Collect(collectible);
            }
        }

        return false;
    }
}
