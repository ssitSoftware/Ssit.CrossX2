using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Bots;

public class AttackWhenCharacterInHitRangeBehavior(SizeF detectorSize, float detectorOffset = 0, float attackPower = 1, Action<ISteeringCharacter> onAttackSuccessful = null) 
    : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var colliderAabb = obj.Body.Colliders[0].Aabb;
        var centerY = colliderAabb.Center.Y;

        Aabb detectorAabb;

        if (obj.FaceLeft)
        {
            detectorAabb = new Aabb(
                colliderAabb.Left - detectorSize.Width - detectorOffset,
                centerY - detectorSize.Height / 2f,
                colliderAabb.Left - detectorOffset,
                centerY + detectorSize.Height / 2f
            );
        }
        else
        {
            detectorAabb = new Aabb(
                colliderAabb.Right + detectorOffset,
                centerY - detectorSize.Height / 2f,
                colliderAabb.Right + detectorSize.Width + detectorOffset,
                centerY + detectorSize.Height / 2f
            );
        }
        
        var colliders = obj.Body.Simulation.GetColliders(detectorAabb, obj.Body, IMaterial.AllColliders, colliderType: ColliderType.Dynamic);

        foreach (var collider in colliders)
        {
            if (collider.AttachedBody?.Owner is not IHittable { Alive: true } hittable)
                continue;

            if (!hittable.IsEnemy(obj))
                continue;
            
            obj.SetSteeringState("Attack");
            obj.CommonSoundContainer?.Play("Slash");
            
            if (hittable.Hit(new Vector2(obj.FaceLeft ? -1 : 1, 0), attackPower))
            {
                onAttackSuccessful?.Invoke(obj);
            }
            return true;
        }

        return false;
    }
}
