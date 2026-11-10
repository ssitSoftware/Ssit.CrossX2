using Ssit.CrossX2.Framework.Games.Logic;
using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;
using Ssit.CrossX2.Framework.Games.Utils;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.BotBrains;

public class AttackWhenInRangeBehavior(BoxHitDetector detector) : Behavior<IBrainCharacter>
{
    protected override void OnLeaveState(IBrainCharacter obj)
    {
        base.OnLeaveState(obj);
        obj.InputController.ResetManualInput();
    }

    protected override bool OnUpdate(IBrainCharacter obj, float dt)
    {
        obj.InputController.SetButtonState(SteeringControlNames.Attack, ButtonState.Empty);
        
        var target = obj.GetParameters<AttackTargetParameters>().Target;
        if (target is not { Alive: true })
            return false;

        var result = detector.Detect(obj.Body, obj.FaceLeft, IMaterial.AllColliders, FilterEnemy, ColliderSides.None);
        if (result.Hittable is not { Alive: true })
            return false;

        obj.InputController.SetValue(SteeringControlNames.HorizontalMove, 0f);
        obj.InputController.SetButtonState(SteeringControlNames.Attack, ButtonState.JustPressed);
        return true;
    }

    private static bool FilterEnemy(IBody body, IHittable hittable)
    {
        return (body.Owner as IHittable)?.IsEnemy(hittable as ISteeringCharacter) ?? false;
    }
}
