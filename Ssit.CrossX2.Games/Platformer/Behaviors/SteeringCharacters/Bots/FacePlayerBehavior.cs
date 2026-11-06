using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Utils;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Bots;

public class FacePlayerBehavior(BoxHitDetector detector, float timeAdjustment) : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var result = detector.Detect(obj.Body, obj.FaceLeft, IMaterial.AllColliders, FilterEnemy);

        if (result.Hittable is not { Alive: true })
            return false;

        obj.FaceLeft = !obj.FaceLeft;
        var parameters = ((ICharacter)obj).GetParameters<EnemyAttackParameters>();
        parameters.LastAttackTime = DateTime.Now - TimeSpan.FromSeconds(timeAdjustment);
        return false;
    }

    private static bool FilterEnemy(IBody body, IHittable hittable)
    {
        return (body.Owner as IHittable)?.IsEnemy(hittable as ISteeringCharacter) ?? false;
    }
}
