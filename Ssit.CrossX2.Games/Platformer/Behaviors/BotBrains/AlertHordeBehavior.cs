using Ssit.CrossX2.Framework.Games.Logic;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.BotBrains;

/// <summary>
/// Brain behavior: when the state is entered with a target, every <see cref="IHordeMember"/> within
/// <paramref name="radius"/> gets alerted about that target. Alerted members do the same when they enter
/// their chase state, so the whole horde wakes up in a ripple. Only members of
/// <paramref name="horde"/> react.
/// </summary>
public class AlertHordeBehavior(float radius, string horde) : Behavior<IBrainCharacter>
{
    protected override void OnEnterState(IBrainCharacter obj)
    {
        HordeAlert.Alert(obj.Body, obj.GetParameters<AttackTargetParameters>().Target, radius, horde);
    }
}
