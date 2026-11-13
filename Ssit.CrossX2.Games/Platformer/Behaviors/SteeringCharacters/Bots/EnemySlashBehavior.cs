using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Logic.Objects.Characters;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Utils;
using Ssit.CrossX2.Framework.Graphics.Sprites;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.SteeringCharacters.Bots;

/// <summary>
/// <see cref="SlashBehavior"/> for enemies: only hits hostile targets (not other enemies)
/// and records the slash time for the attack cooldown in <see cref="EnemyAttackParameters"/>.
/// </summary>
public class EnemySlashBehavior(BoxHitDetector detector, params (string, float)[] powers) : SlashBehavior(detector, powers)
{
    protected override bool OnEvent(ISteeringCharacter obj, ISpriteEvent @event)
    {
        if (IsAttackEvent(@event.EventName))
        {
            var parameters = obj.GetParameters<EnemyAttackParameters>();
            parameters.LastAttackTime = DateTime.Now;
        }
        return base.OnEvent(obj, @event);
    }

    protected override bool Filter(IBody body, IHittable hittable) =>
        (body.Owner as IHittable)?.IsEnemy(hittable as ISteeringCharacter) ?? false;
}