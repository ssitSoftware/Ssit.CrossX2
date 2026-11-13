using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Utils;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Bots;

/// <summary>
/// Turns the enemy around when a hostile target is detected (the detector is typically placed behind the enemy).
/// Also shifts the last attack time by timeAdjustment so the attack cooldown is shortened (or extended if negative) after turning.
/// </summary>
public class FacePlayerBehavior(BoxHitDetector detector, float timeAdjustment) : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var result = detector.Detect(obj.Body, obj.FaceLeft, IMaterial.AllColliders, FilterEnemy);

        if (result.Hittable is not { Alive: true })
            return false;

        obj.FaceLeft = !obj.FaceLeft;
        var parameters = obj.GetParameters<EnemyAttackParameters>();
        // Pretend the last attack was timeAdjustment seconds ago so the enemy can strike sooner after turning
        parameters.LastAttackTime = DateTime.Now - TimeSpan.FromSeconds(timeAdjustment);
        return false;
    }

    /// <summary>
    /// Accepts only hittables that are hostile to the owner of the body.
    /// </summary>
    private static bool FilterEnemy(IBody body, IHittable hittable)
    {
        return (body.Owner as IHittable)?.IsEnemy(hittable as ISteeringCharacter) ?? false;
    }
}
