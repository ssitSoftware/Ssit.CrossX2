using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Parameters;
using Ssit.CrossX2.Framework.Games.Platformer.Helpers;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class LandingTransitionBehavior(string onGroundState, CheckAdditionalGroundHelper additionalGroundHelper) : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        if (!additionalGroundHelper.IsOnGroundExtra(obj))
            return false;

        obj.GetParameters<LandingParameters>(true).Velocity = obj.Body.Velocity;
        obj.SetSteeringState(onGroundState);
        return true;
    }
}
