using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

/// <summary>
/// Switches from rising to the fall state once the vertical velocity is no longer upward (the jump apex).
/// </summary>
public class RaiseToFallBehavior(string fallState = "Fall") : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        if (obj.Body.Velocity.Y < 0)
            return false;

        obj.SetSteeringState(fallState);
        return true;
    }
}
