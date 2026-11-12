using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Platformer.Helpers;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Starts a jump (enters jumpState) when Jump is pressed and the character stands on ground, including extra ground detection (e.g. coyote time).
/// </summary>
public class JumpBehavior(CheckAdditionalGroundHelper additionalGroundHelper, string jumpState = "Raise") : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        if (obj.SteeringInput.Button(SteeringControlNames.Jump) != ButtonState.JustPressed)
            return false;
        
        if (!additionalGroundHelper.IsOnGroundExtra(obj))
            return false;

        obj.SoundContainer?.Play("Jump", obj.SteeringParameters.GroundMaterial);
        obj.SetSteeringState(jumpState);
        return true;
    }
}
