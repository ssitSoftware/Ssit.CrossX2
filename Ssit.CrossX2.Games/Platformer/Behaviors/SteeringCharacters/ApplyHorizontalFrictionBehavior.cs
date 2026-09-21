using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Parameters;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class ApplyHorizontalFrictionBehavior(float frictionMultiplier) : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        if (!obj.SteeringParameters.IsOnStaticGround)
            return false;

        var landingParameters = obj.GetParameters<LandingParameters>(true);
        if (landingParameters.Velocity is { } landingVelocity)
        {
            var velocity = obj.Body.Velocity;
            if (velocity.X == 0f
                || MathF.Sign(velocity.X) != MathF.Sign(landingVelocity.X)
                || MathF.Abs(velocity.X) > MathF.Abs(landingVelocity.X))
            {
                landingParameters.Velocity = null;
            }
        }
        
        var factor = MathF.Max(0, MathF.Min(1, landingParameters.Velocity.GetValueOrDefault().Y / obj.PhysicsValues.RunSpeed / 2));
        factor *= factor;
        var landingFrictionModifier =  factor * obj.PhysicsValues.FrictionModifierOnLandingFactor + 1 - factor;

        var colliders = obj.Body.Colliders;
        PhysicsUtils.ApplyHorizontalFriction(colliders[0], obj.SteeringParameters.GroundMaterial, dt * frictionMultiplier * landingFrictionModifier);

        return false;
    }
}
