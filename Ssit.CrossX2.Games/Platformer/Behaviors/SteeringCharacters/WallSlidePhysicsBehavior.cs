using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class WallSlidePhysicsBehavior : SteeringBehavior<ISteeringCharacter>
{
    private class Parameters
    {
        public float CurrentMaxSpeed = 0;
    }

    protected override void OnEnter(ISteeringCharacter character)
    {
        base.OnEnter(character);

        var parameters = ((ICharacter)character).GetParameters<Parameters>();
        parameters.CurrentMaxSpeed = 0;
        character.Body.Velocity = Vector2.Zero;
    }

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var parameters = ((ICharacter)obj).GetParameters<Parameters>();
        
        parameters.CurrentMaxSpeed += dt * obj.PhysicsValues.WallSlideSpeed / obj.PhysicsValues.WallSlideTimeToFullSpeed;
        parameters.CurrentMaxSpeed = MathF.Min(obj.PhysicsValues.WallSlideSpeed, parameters.CurrentMaxSpeed);

        obj.Body.Velocity = new Vector2(0, parameters.CurrentMaxSpeed);

        return false;
    }
}
