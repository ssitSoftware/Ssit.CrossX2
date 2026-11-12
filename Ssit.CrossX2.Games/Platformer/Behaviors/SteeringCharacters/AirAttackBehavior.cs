using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Attack in mid-air: on an Attack press, overrides the velocity so the character dives down and switches to the "AirAttack" state.
/// horizontalMoveDivider dampens the current horizontal speed; addHorizontalVelocityFactor adds a push in the facing direction
/// (a negative value pushes backwards and flips the facing, i.e. a recoil-style attack).
/// </summary>
public class AirAttackBehavior(float horizontalMoveDivider = 1, float addHorizontalVelocityFactor = 0) : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        if (obj.SteeringInput.Button(SteeringControlNames.Attack) != ButtonState.JustPressed)
            return false;

        obj.Body.Velocity = obj.Body.Velocity
            with
            {
                // Damp existing horizontal speed and add the extra push relative to the facing direction
                X = obj.Body.Velocity.X / horizontalMoveDivider + addHorizontalVelocityFactor * (obj.FaceLeft ? -1 : 1),
                Y = obj.PhysicsValues.AirAttackDownVelocity
            };

        // A backward push turns the character around to face where it is moving
        if (addHorizontalVelocityFactor < 0)
        {
            obj.FaceLeft = !obj.FaceLeft;
        }
        
        obj.SoundContainer?.Play("Attack");
        obj.SetSteeringState("AirAttack");
        
        return true;
    }
}


