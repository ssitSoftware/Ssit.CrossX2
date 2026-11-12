using Ssit.CrossX2.Framework.Games.Logic;
using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;
using Ssit.CrossX2.Framework.Games.Utils;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.BotBrains;

/// <summary>
/// Brain behavior for an enemy pretending to be a harmless NPC. It stands still and only reacts when the target
/// has walked past it (is inside <paramref name="behindDetector"/>) or bumps right into it
/// (<paramref name="closeDetector"/>). Then the target is remembered and the brain switches to <paramref name="revealStateName"/>.
/// </summary>
public class WaitDisguisedBehavior(BoxHitDetector behindDetector, BoxHitDetector closeDetector, string revealStateName) : Behavior<IBrainCharacter>
{
    protected override void OnEnterState(IBrainCharacter obj)
    {
        obj.InputController.ResetManualInput();
    }

    protected override bool OnUpdate(IBrainCharacter obj, float dt)
    {
        obj.InputController.SetValue(SteeringControlNames.HorizontalMove, 0f);

        // Behind takes priority, then right next to the bot
        var target = Detect(behindDetector, obj) ?? Detect(closeDetector, obj);
        if (target is null)
            return false;

        obj.GetParameters<AttackTargetParameters>().Target = target;
        obj.SetBrainState(revealStateName);
        return true;
    }

    /// <summary>
    /// Returns the live hostile hittable inside the detector box, or null.
    /// </summary>
    private static IHittable Detect(BoxHitDetector detector, IBrainCharacter obj)
    {
        var result = detector.Detect(obj.Body, obj.FaceLeft, IMaterial.AllColliders, FilterEnemy, ColliderSides.None);
        return result.Hittable is { Alive: true } hittable ? hittable : null;
    }

    /// <summary>
    /// Accepts only hittables that are hostile to the owner of the body.
    /// </summary>
    private static bool FilterEnemy(IBody body, IHittable hittable)
    {
        return (body.Owner as IHittable)?.IsEnemy(hittable as ISteeringCharacter) ?? false;
    }
}
