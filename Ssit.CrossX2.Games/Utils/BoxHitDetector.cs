using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Utils;

/// <param name="triggerGroups">
/// Collider groups (<see cref="IMaterial.Bit"/> mask) of triggers that are hit-tested too. When non-zero, triggers are
/// queried as well, but only triggers of these groups are considered (e.g. sword-only targets like berry bushes).
/// </param>
public class BoxHitDetector(Vector2 offset, SizeF size, ColliderType colliderType = ColliderType.Static | ColliderType.Dynamic, ulong triggerGroups = 0)
{
    private readonly List<ICollider> _colliderBuffer = new();

    private static bool DefaultFilter(IBody _1, IHittable _) => true;

    /// <summary>
    /// Optional extra detector checked only when this one found no <see cref="IHittable"/> (e.g. a box below the main one,
    /// so attacks reach targets standing a bit lower). Only a hittable found by the fallback replaces this detector's result.
    /// </summary>
    public BoxHitDetector Fallback { get; init; }

    /// <summary>
    /// When true, non-static colliders (dynamic bodies, triggers) are searched for a hittable first, and only when none is
    /// found the regular nearest-first search runs (static colliders included). So a wall between the attacker and a target
    /// that's still inside the box doesn't swallow the hit.
    /// </summary>
    public bool PreferDynamic { get; init; }

    public HitTestResult Detect(IBody body, bool faceLeft, ulong collidesWith, Func<IBody, IHittable, bool> filter = null, ColliderSides bodylessSides = ColliderSides.All)
    {
        filter ??= DefaultFilter;

        var result = DetectSelf(body, faceLeft, collidesWith, filter, bodylessSides);
        if (result.Hittable != null || Fallback == null)
            return result;

        // No hittable in the main box - look for one in the fallback box (bodyless colliders are skipped there).
        var fallbackResult = Fallback.Detect(body, faceLeft, collidesWith, filter, ColliderSides.None);
        return fallbackResult.Hittable != null ? fallbackResult : result;
    }

    private HitTestResult DetectSelf(IBody body, bool faceLeft, ulong collidesWith, Func<IBody, IHittable, bool> filter, ColliderSides bodylessSides)
    {
        QueryAndSort(body, faceLeft, collidesWith);

        if (PreferDynamic)
        {
            // Dynamic first: the nearest hittable that isn't on a static collider wins, even if a wall is closer.
            foreach (var collider in _colliderBuffer)
            {
                if ((collider.Type & ColliderType.Static) != 0 || !IsCandidate(collider))
                    continue;

                if (collider.AttachedBody?.Owner is IHittable { Alive: true } hittable && filter(body, hittable))
                    return new HitTestResult { HitAnything = true, Collider = collider, Hittable = hittable };
            }
        }

        foreach (var collider in _colliderBuffer)
        {
            if (!IsCandidate(collider))
                continue;
            
            if (collider.AttachedBody?.Owner is IHittable { Alive: true } hittable && filter(body, hittable))
                return new HitTestResult { HitAnything = true, Collider = collider, Hittable = hittable };

            if (collider.Type is ColliderType.Static or ColliderType.Dynamic && 0 != (collider.Material.Sides & bodylessSides))
                return new HitTestResult { HitAnything = true, Collider = collider, Hittable = null };
        }

        return HitTestResult.None;
    }

    private bool IsCandidate(ICollider collider)
    {
        if (!collider.IsActive)
            return false;

        return triggerGroups == 0 || (collider.Type & ColliderType.Trigger) == 0
            || (triggerGroups & IMaterial.Bit(collider.Material.ColliderGroup)) != 0;
    }

    public Aabb GetAabb(IBody body, bool faceLeft)
    {
        var off = offset;
        if (faceLeft) off.X = -off.X;
        return new Aabb(body.Position + off, size);
    }
    
    private void QueryAndSort(IBody body, bool faceLeft, ulong collidesWith)
    {
        var aabb = GetAabb(body, faceLeft);

        _colliderBuffer.Clear();
        body.Simulation.CheckCollision(aabb, body, collidesWith: collidesWith, colliders: _colliderBuffer, colliderType: triggerGroups != 0 ? colliderType | ColliderType.Trigger : colliderType, debugRegister: true);

        _colliderBuffer.Sort((a, b) =>
            faceLeft ? b.Aabb.Right.CompareTo(a.Aabb.Right) : a.Aabb.Left.CompareTo(b.Aabb.Left));
    }
}
