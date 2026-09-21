using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class AirAttackBehavior(float horizontalMoveDivider = 1, float addHorizontalVelocityFactor = 0) : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        if (obj.SteeringInput.Button(SteeringControlNames.Attack) != ButtonState.JustPressed)
            return false;

        obj.Body.Velocity = obj.Body.Velocity
            with
            {
                X = obj.Body.Velocity.X / horizontalMoveDivider + addHorizontalVelocityFactor * (obj.FaceLeft ? -1 : 1),
                Y = obj.PhysicsValues.AirAttackDownVelocity
            };

        if (addHorizontalVelocityFactor < 0)
        {
            obj.FaceLeft = !obj.FaceLeft;
        }
        
        obj.SoundContainer?.Play("Attack");
        obj.SetSteeringState("AirAttack");
        
        return true;
    }
}


