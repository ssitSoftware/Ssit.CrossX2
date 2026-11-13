using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Logic.Stering;
using Ssit.CrossX2.Framework.Games.Platformer.Behaviors.BotBrains;
using Ssit.CrossX2.Framework.Graphics.Sprites;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Bots;

/// <summary>
/// Steering behavior: on any of the given sprite events (e.g. "Bark"), alerts every <see cref="IHordeMember"/>
/// of <paramref name="horde"/> within <paramref name="radius"/> about the brain's current target
/// (<see cref="AttackTargetParameters"/>). Does nothing when there is no live target.
/// </summary>
public class AlertHordeOnEventBehavior(float radius, string horde, params string[] eventNames) : SteeringBehavior<ISteeringCharacter>
{
    protected override bool OnEvent(ISteeringCharacter obj, ISpriteEvent @event)
    {
        if (Array.IndexOf(eventNames, @event.EventName) < 0)
            return false;

        var target = obj.GetParameters<AttackTargetParameters>().Target;
        HordeAlert.Alert(obj.Body, target, radius, horde);
        return false;
    }
}
