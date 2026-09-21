using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class DownwardThrustBehavior : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        if (obj.SteeringInput.Button(SteeringControlNames.Attack) != ButtonState.JustPressed)
            return false;

        if (!obj.SteeringInput.Button(SteeringControlNames.Jump).IsDown)
            return false;

        obj.Body.Velocity = obj.Body.Velocity with { X = 0, Y = obj.PhysicsValues.ThrustDownVelocity };
        obj.SetSteeringState("Thrust");
        return true;
    }
}
