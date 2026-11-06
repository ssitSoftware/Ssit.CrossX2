using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class JumpAttackBehavior : SteeringBehavior<ISteeringCharacter>
{
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class Parameters
    {
        public bool CanJumpAttack;
    }
    
    protected override void OnEnter(ISteeringCharacter character)
    {
        base.OnEnter(character);

        var parameters = ((ICharacter)character).GetParameters<Parameters>();
        parameters.CanJumpAttack = character.SteeringInput.Button(SteeringControlNames.Jump).IsDown; 
    }

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var parameters = ((ICharacter)obj).GetParameters<Parameters>();
        parameters.CanJumpAttack &= obj.SteeringInput.Button(SteeringControlNames.Jump).IsDown;
        
        if (!parameters.CanJumpAttack || obj.SteeringInput.Button(SteeringControlNames.Attack) != ButtonState.JustPressed )
            return false;

        obj.Body.Velocity = obj.Body.Velocity with { Y = -obj.PhysicsValues.JumpAttackRaiseVelocity };
        
        obj.SoundContainer?.Play("JumpCombo");
        obj.SetSteeringState("JumpCombo");
        return true;
    }
}
