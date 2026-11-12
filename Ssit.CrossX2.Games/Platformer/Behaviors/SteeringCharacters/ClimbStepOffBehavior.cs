using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Lets the character let go of a climbable surface by pressing Jump (unless pushing toward the surface).
/// It is nudged away from the surface, given a small outward velocity of jumpOffSpeed and enters "Fall".
/// </summary>
public class ClimbStepOffBehavior(float jumpOffSpeed) : SteeringBehavior<ISteeringCharacter>
{
    private const float StepOffDistance = 0.1f;

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var movingAway =
            obj.SteeringInput.Button(SteeringControlNames.Jump) == ButtonState.JustPressed; 
        
        if (!movingAway)
            return false;
        
        var sign = obj.FaceLeft ? -1 : 1;
        var move = obj.SteeringInput.Value(SteeringControlNames.HorizontalMove);

        // Pushing away from the facing direction is not a step-off here - ignore the press
        if (MathF.Sign(move) != sign && MathF.Abs(move) > 0.1f)
        {
            return false;
        }
        
        // Move away from the surface (opposite to the facing direction) so the body does not stay stuck to it
        var shift = obj.FaceLeft ? StepOffDistance : -StepOffDistance;
        obj.Body.Position += new Vector2(shift, 0);
        obj.Body.Velocity = new Vector2(obj.FaceLeft ? jumpOffSpeed : -jumpOffSpeed, 0);
        obj.SetSteeringState("Fall");
        return true;
    }
}
