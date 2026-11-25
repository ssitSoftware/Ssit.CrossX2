namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

/// <summary>
/// Object the player can pick up. Found by checking the collector's collider AABB against
/// colliders whose owner implements this interface.
/// </summary>
public interface ICollectible
{
    /// <summary>Collectible type (a game-defined value, e.g. silver / health), readable before collecting.</summary>
    byte CollectibleType { get; }

    /// <summary>False while the collectible can't be picked up (e.g. right after it was spawned, or already collected).</summary>
    bool CanBeCollected { get; }

    /// <summary>
    /// Collects the object - it disappears (its body is removed from the simulation).
    /// </summary>
    /// <returns>Collectible type (see <see cref="CollectibleType"/>).</returns>
    byte Collect();
}

/// <summary>
/// Object that can collect <see cref="ICollectible"/>s.
/// </summary>
public interface ICollector
{
    /// <summary>
    /// Tries to collect the collectible.
    /// </summary>
    /// <returns>False when the collector refuses it - then no collection happens and the collectible stays.</returns>
    bool Collect(ICollectible collectible);
}
