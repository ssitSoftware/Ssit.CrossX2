using System.Numerics;

namespace Ssit.CrossX2.Framework.Games.Physics;

public interface ICollider: IAabbObject
{
    /// <summary>Collider group used by terrain (static map colliders, map bounds).</summary>
    public const byte GroupTerrain = 0;

    event CollisionDelegate CollisionWith;
    string Name { get; }
    IBody AttachedBody { get; }
    ColliderType Type { get; }
    IMaterial Material { get; }
    bool IsActive { get; set; }
    Aabb GetAabb(Vector2 position);
    void RaiseCollisionWith(bool byMyMovement, ICollider other, Vector2 impact);
    bool GetMovementCollision(ICollider obstacle, ref Vector2 move, out Vector2 normal);
    bool CheckCollisionWith(ICollider obstacle);
}