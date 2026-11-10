using Ssit.CrossX2.Framework.Games.Logic;
using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Utils;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.BotBrains;

public class ScanForAttackBehavior(BoxHitDetector detector, string chaseStateName) : Behavior<IBrainCharacter>
{
    protected override bool OnUpdate(IBrainCharacter obj, float dt)
    {
        var parameters = obj.GetParameters<AttackTargetParameters>();
        if (parameters.IgnoreTimeRemaining > 0)
        {
            parameters.IgnoreTimeRemaining -= dt;
        }

        var result = detector.Detect(obj.Body, obj.FaceLeft, IMaterial.AllColliders, FilterEnemy, ColliderSides.None);

        if (result.Hittable is not { Alive: true } hittable || parameters.IsIgnored(hittable))
            return false;

        parameters.SetTarget(hittable);
        obj.SetBrainState(chaseStateName);
        return true;
    }

    private static bool FilterEnemy(IBody body, IHittable hittable)
    {
        return (body.Owner as IHittable)?.IsEnemy(hittable as ISteeringCharacter) ?? false;
    }
}
