using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class AttackBehavior(string control, string attackStateName, string attackSoundName =  null) : SteeringBehavior<ISteeringCharacter>
{
    public class AttackingData
    {
        public float NextAttackTimeout;
    }

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var parameters = obj.GetParameters<AttackingData>(true);
        if (parameters.NextAttackTimeout > 0)
        {
            parameters.NextAttackTimeout -= dt;
            return false;
        }
        
        if (obj.SteeringInput.Button(control) != ButtonState.JustPressed)
            return false;

        obj.Body.Velocity = obj.Body.Velocity
            with { X = obj.FaceLeft ? 
                -obj.PhysicsValues.AttackVelocity : obj.PhysicsValues.AttackVelocity };
        
        obj.SoundContainer?.Play(attackSoundName ?? attackStateName);
        obj.SetSteeringState(attackStateName);
        return true;
    }
}
