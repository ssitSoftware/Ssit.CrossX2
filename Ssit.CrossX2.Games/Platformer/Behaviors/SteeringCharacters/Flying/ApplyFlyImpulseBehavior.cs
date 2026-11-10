using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Flying;

/// <summary>
/// Sets velocity when the state is entered. X is relative to facing (positive = forward), Y is absolute (negative = up).
/// </summary>
public class ApplyFlyImpulseBehavior(Vector2 impulse) : SteeringBehavior<ISteeringCharacter>
{
    protected override void OnEnter(ISteeringCharacter obj)
    {
        obj.Body.Velocity = new Vector2(obj.FaceLeft ? -impulse.X : impulse.X, impulse.Y);
    }
}
