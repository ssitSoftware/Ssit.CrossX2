using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Jump off a wall: on Jump press the character turns away from the wall and is pushed sideways with jumpHorizontalVelocity,
/// then enters "Raise".
/// </summary>
public class WallJumpBehavior(float jumpHorizontalVelocity) : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        if (obj.SteeringInput.Button(SteeringControlNames.Jump) != ButtonState.JustPressed)
            return false;

        // While on a wall the character faces the wall, so flip to face away
        obj.FaceLeft = !obj.FaceLeft;
        obj.Body.Velocity = new Vector2(obj.FaceLeft ? -jumpHorizontalVelocity : jumpHorizontalVelocity, 0);
        obj.SteeringParameters.LastHorizontalVelocity = obj.Body.Velocity.X;
        
        obj.SoundContainer?.Play("WallJump");
        obj.SetSteeringState("Raise");
        return true;
    }
}
