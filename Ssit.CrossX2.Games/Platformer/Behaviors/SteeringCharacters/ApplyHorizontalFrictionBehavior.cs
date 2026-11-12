using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Parameters;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Slows the character down on static ground using the ground material's friction (no input handling).
/// frictionMultiplier scales the friction; right after a hard landing the friction is additionally modified
/// (FrictionModifierOnLandingFactor), so the character slides or stops depending on the landing speed.
/// </summary>
public class ApplyHorizontalFrictionBehavior(float frictionMultiplier) : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        if (!obj.SteeringParameters.IsOnStaticGround)
            return false;

        var landingParameters = ((ICharacter)obj).GetParameters<LandingParameters>();
        // Forget the stored landing velocity once the character has stopped, reversed or become faster than at landing
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
        
        // How hard the landing was (0..1, based on vertical landing speed), squared to emphasize hard landings
        var factor = MathF.Max(0, MathF.Min(1, landingParameters.Velocity.GetValueOrDefault().Y / obj.PhysicsValues.RunSpeed / 2));
        factor *= factor;
        // Blend between normal friction (factor 0) and the landing friction modifier (factor 1)
        var landingFrictionModifier =  factor * obj.PhysicsValues.FrictionModifierOnLandingFactor + 1 - factor;

        var colliders = obj.Body.Colliders;
        PhysicsUtils.ApplyHorizontalFriction(colliders[0], obj.SteeringParameters.GroundMaterial, dt * frictionMultiplier * landingFrictionModifier);

        return false;
    }
}
