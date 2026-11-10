using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Platformer.Behaviors.BotBrains;

public static class HordeAlert
{
    /// <summary>
    /// Alerts every <see cref="IHordeMember"/> within <paramref name="radius"/> of <paramref name="body"/>
    /// (except the body's owner) about <paramref name="target"/>. Only members of <paramref name="horde"/> react.
    /// </summary>
    public static void Alert(IBody body, IHittable target, float radius, string horde)
    {
        if (target is not { Alive: true })
            return;

        var position = body.Position;
        var area = new Aabb(position.X - radius, position.Y - radius, position.X + radius, position.Y + radius);

        var colliders = body.Simulation.GetColliders(area, body, IMaterial.AllColliders, colliderType: ColliderType.Dynamic);

        // Copy first - alerting changes other members' states, which must not interfere with the query buffer.
        var members = colliders
            .Select(c => c.AttachedBody?.Owner as IHordeMember)
            .Where(m => m != null && !ReferenceEquals(m, body.Owner))
            .Distinct()
            .ToList();

        foreach (var member in members)
        {
            member.Alert(target, horde);
        }
    }
}
