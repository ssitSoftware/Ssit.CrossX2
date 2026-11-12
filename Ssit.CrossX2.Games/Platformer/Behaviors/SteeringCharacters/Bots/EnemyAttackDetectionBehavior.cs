using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Utils;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Bots;

/// <summary>
/// Enemy attack trigger: when an enemy of this character (e.g. the player) is alive inside the detector box
/// and at least timeBetweenAttacks seconds passed since the last attack, lunges forward and enters attackStateName.
/// </summary>
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

    /// <summary>
    /// Accepts only hittables that are hostile to the owner of the body.
    /// </summary>
    private static bool FilterEnemy(IBody body, IHittable hittable)
    {
        return (body.Owner as IHittable)?.IsEnemy(hittable as ISteeringCharacter) ?? false;
    }
}
