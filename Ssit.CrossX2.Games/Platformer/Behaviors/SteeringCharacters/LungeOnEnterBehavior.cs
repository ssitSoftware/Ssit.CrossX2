using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// On entering the state, lunges forward (facing direction) with AttackVelocity * <paramref name="velocityMultiplier"/>.
/// Works however the state is entered - from <see cref="AttackBehavior"/> (replacing its lunge) or as a combo follow-up.
/// </summary>
public class LungeOnEnterBehavior(float velocityMultiplier) : SteeringBehavior<ISteeringCharacter>
{
    protected override void OnEnter(ISteeringCharacter obj)
    {
        var velocity = obj.PhysicsValues.AttackVelocity * velocityMultiplier;
        obj.Body.Velocity = obj.Body.Velocity with { X = obj.FaceLeft ? -velocity : velocity };
    }
}
