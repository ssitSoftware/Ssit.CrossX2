using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class ApplyVerticalJumpVelocityBehavior : SteeringBehavior<ISteeringCharacter>
{
    protected override void OnEnter(ISteeringCharacter obj)
    {
        obj.SteeringParameters.LastHorizontalVelocity = obj.Body.Velocity.X;

        if (MathF.Abs(obj.Body.Velocity.X) < 0.1f)
        {
            obj.SteeringParameters.LastHorizontalVelocity = obj.FaceLeft ? -0.1f : 0.1f;
        }

        obj.Body.Velocity = obj.Body.Velocity with { Y = -obj.PhysicsValues.JumpVelocity };
        obj.Body.Velocity += obj.Body.KinematicVelocity with { Y = 0 };

        obj.Body.Position -= new Vector2(0, 0.22f);
        obj.SteeringParameters.IsOnGround = false;
    }
}