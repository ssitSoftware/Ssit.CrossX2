using System.Numerics;
using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Utils;

public class BoxHitDetector(Vector2 offset, SizeF size, ColliderType colliderType = ColliderType.Static | ColliderType.Dynamic)
{
    private readonly List<ICollider> _colliderBuffer = new();

    private static bool DefaultFilter(IBody _1, IHittable _) => true;
    
    public HitTestResult Detect(IBody body, bool faceLeft, ulong collidesWith, Func<IBody, IHittable, bool> filter = null)
    {
        filter ??= DefaultFilter;

        QueryAndSort(body, faceLeft, collidesWith);

        foreach (var collider in _colliderBuffer)
        {
            if (!collider.IsActive)
                continue;
            
            if (collider.AttachedBody?.Owner is IHittable { Alive: true } hittable && filter(body, hittable))
                return new HitTestResult { HitAnything = true, Collider = collider, Hittable = hittable };

            if (collider.Type is ColliderType.Static or ColliderType.Dynamic)
                return new HitTestResult { HitAnything = true, Collider = collider, Hittable = null };
        }

        return HitTestResult.None;
    }

    private void QueryAndSort(IBody body, bool faceLeft, ulong collidesWith)
    {
        var off = offset;
        if (faceLeft) off.X = -off.X;

        var aabb = new Aabb(body.Position + off, size);

        _colliderBuffer.Clear();
        body.Simulation.CheckCollision(aabb, body, collidesWith: collidesWith, colliders: _colliderBuffer, colliderType: colliderType, debugRegister: true);

        _colliderBuffer.Sort((a, b) =>
            faceLeft ? b.Aabb.Right.CompareTo(a.Aabb.Right) : a.Aabb.Left.CompareTo(b.Aabb.Left));
    }
}
