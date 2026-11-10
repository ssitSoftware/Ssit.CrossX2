using Ssit.CrossX2.Framework.Games.Logic;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.BotBrains;

/// <summary>
/// Brain behavior that drops the disguise: turns towards the target, plays the steering
/// <paramref name="revealSteeringState"/> (the steering state is responsible for returning to Idle when the
/// sequence ends) and moves the brain to <paramref name="chaseStateName"/>, or <paramref name="noTargetStateName"/>
/// when there is nobody to chase. Movement input given while the reveal animation plays is ignored by the steering
/// state, so chasing starts right after the animation.
/// </summary>
public class RevealBehavior(string revealSteeringState, string chaseStateName, string noTargetStateName) : Behavior<IBrainCharacter>
{
    protected override bool OnUpdate(IBrainCharacter obj, float dt)
    {
        obj.InputController.SetValue(SteeringControlNames.HorizontalMove, 0f);

        var target = obj.GetParameters<AttackTargetParameters>().Target;
        if (target is { Alive: true })
        {
            obj.FaceLeft = target.Position.X < obj.Body.Position.X;
        }

        (obj as ISteeringCharacter)?.SetSteeringState(revealSteeringState);

        obj.SetBrainState(target is { Alive: true } ? chaseStateName : noTargetStateName);
        return true;
    }
}
