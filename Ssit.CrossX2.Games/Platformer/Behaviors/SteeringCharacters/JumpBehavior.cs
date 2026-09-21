using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Platformer.Helpers;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class JumpBehavior(CheckAdditionalGroundHelper additionalGroundHelper) : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        if (obj.SteeringInput.Button(SteeringControlNames.Jump) != ButtonState.JustPressed)
            return false;
        
        if (!additionalGroundHelper.IsOnGroundExtra(obj))
            return false;

        obj.SoundContainer?.Play("Jump");
        obj.SetSteeringState("Raise");
        return true;
    }
}
