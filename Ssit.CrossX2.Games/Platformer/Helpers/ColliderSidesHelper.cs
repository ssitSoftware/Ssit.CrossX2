using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Platformer.Helpers;

public static class ColliderSidesHelper
{
    /// <summary>
    /// True if the collider blocks a body moving horizontally into it - i.e. it has the side facing the mover
    /// (Left side when moving right, Right side when moving left). One-way platforms (Top only) never block.
    /// </summary>
    public static bool BlocksHorizontalMove(this ICollider collider, bool movingLeft) =>
        collider.Material.Sides.HasFlag(movingLeft ? ColliderSides.Right : ColliderSides.Left);

    /// <summary>True if the collider can be stood on (has a Top side).</summary>
    public static bool IsGround(this ICollider collider) =>
        collider.Material.Sides.HasFlag(ColliderSides.Top);
}
