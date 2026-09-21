using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

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

        if (MathF.Sign(move) != sign && MathF.Abs(move) > 0.1f)
        {
            return false;
        }
        
        var shift = obj.FaceLeft ? StepOffDistance : -StepOffDistance;
        obj.Body.Position += new Vector2(shift, 0);
        obj.Body.Velocity = new Vector2(obj.FaceLeft ? jumpOffSpeed : -jumpOffSpeed, 0);
        obj.SetSteeringState("Fall");
        return true;
    }
}
