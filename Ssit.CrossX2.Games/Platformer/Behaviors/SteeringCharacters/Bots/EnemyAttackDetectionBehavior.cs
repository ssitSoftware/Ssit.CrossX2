using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Utils;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Bots;

public class EnemyAttackParameters
{
    public DateTime LastAttackTime = DateTime.Now;
}

public class EnemyAttackDetectionBehavior(BoxHitDetector detector, string attackStateName, float timeBetweenAttacks) : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var parameters = ((ICharacter)obj).GetParameters<EnemyAttackParameters>();
        
        var delta = DateTime.Now - parameters.LastAttackTime;
        if (delta.TotalSeconds < timeBetweenAttacks)
            return false;
        
        var result = detector.Detect(obj.Body, obj.FaceLeft, IMaterial.AllColliders, FilterEnemy);
        
        if (result.Hittable is not { Alive: true })
            return false;

        obj.Body.Velocity = obj.Body.Velocity
            with { X = obj.FaceLeft ? 
                -obj.PhysicsValues.AttackVelocity : obj.PhysicsValues.AttackVelocity };
        
        obj.SetSteeringState(attackStateName);
        return true;
    }

    private static bool FilterEnemy(IBody body, IHittable hittable)
    {
        return (body.Owner as IHittable)?.IsEnemy(hittable as ISteeringCharacter) ?? false;
    }
}
