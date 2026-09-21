using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters;

public class AutoRunBehavior : SteeringBehavior<ISteeringCharacter>
{
    protected override void OnExit(ISteeringCharacter obj)
    {
        base.OnExit(obj);
        obj.SoundContainer?.StopLoop("Run");
    }

    protected override bool OnFixedUpdate(ISteeringCharacter obj, float dt)
    {
        obj.SoundContainer?.PlayLoop("Run");
        obj.Body.Velocity = obj.Body.Velocity with { X = obj.SteeringInput.Value(SteeringControlNames.HorizontalMove) * obj.PhysicsValues.RunSpeed };
        return false;
    }
}
