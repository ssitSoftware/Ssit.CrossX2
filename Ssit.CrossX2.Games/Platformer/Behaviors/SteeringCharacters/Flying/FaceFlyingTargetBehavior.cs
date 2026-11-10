using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Flying;

/// <summary>Keeps the object turned towards its current target.</summary>
public class FaceFlyingTargetBehavior : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        var target = obj.FlyingParameters().Target;
        if (target is not null)
        {
            obj.FaceLeft = target.Position.X < obj.Body.Position.X;
        }
        return false;
    }
}
