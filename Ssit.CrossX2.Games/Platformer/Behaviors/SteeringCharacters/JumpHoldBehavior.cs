using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class JumpHoldBehavior : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        if (obj.Body.Velocity.Y >= 0 || !obj.SteeringInput.Button(SteeringControlNames.Jump).IsDown)
            return false;

        var physicsValues = obj.PhysicsValues;
        var acc = physicsValues.JumpHoldAccelFactor * physicsValues.JumpVelocity
                  + physicsValues.JumpFactor * physicsValues.JumpHoldAccelInc * physicsValues.JumpVelocity;
        obj.Body.Velocity += new Vector2(0, -acc * dt);
        return false;
    }
}
