using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class ApplyJumpVelocityBehavior : SteeringBehavior<ISteeringCharacter>
{
    protected override void OnEnter(ISteeringCharacter obj)
    {
        obj.Body.Velocity = obj.Body.Velocity with { Y = -obj.PhysicsValues.JumpVelocity };
        obj.Body.Velocity = obj.Body.Velocity with { X = (obj.FaceLeft ? -obj.PhysicsValues.RunSpeed : obj.PhysicsValues.RunSpeed) + obj.Body.KinematicVelocity.X };
        obj.SteeringParameters.LastHorizontalVelocity = obj.Body.Velocity.X;

        obj.Body.Position -= new Vector2(0, 0.22f);
        obj.SteeringParameters.IsOnGround = false;
    }
}