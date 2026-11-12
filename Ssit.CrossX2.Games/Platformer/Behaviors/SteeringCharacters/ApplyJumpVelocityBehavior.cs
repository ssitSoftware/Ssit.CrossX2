using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// On entering the state, launches the character upward (a jump takeoff) with run speed in the facing direction
/// plus any velocity from the platform it stands on.
/// </summary>
public class ApplyJumpVelocityBehavior : SteeringBehavior<ISteeringCharacter>
{
    protected override void OnEnter(ISteeringCharacter obj)
    {
        obj.Body.Velocity = obj.Body.Velocity with { Y = -obj.PhysicsValues.JumpVelocity };
        obj.Body.Velocity = obj.Body.Velocity with { X = (obj.FaceLeft ? -obj.PhysicsValues.RunSpeed : obj.PhysicsValues.RunSpeed) + obj.Body.KinematicVelocity.X };
        obj.SteeringParameters.LastHorizontalVelocity = obj.Body.Velocity.X;

        // Lift the body slightly so it leaves the ground contact immediately and is not snapped back to the ground
        obj.Body.Position -= new Vector2(0, 0.22f);
        obj.SteeringParameters.IsOnGround = false;
    }
}