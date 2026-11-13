namespace Ssit.CrossX2.Framework.Games.Platformer.Builders;

/// <summary>
/// Object created from a map file entry.
/// </summary>
/// <param name="MapId">Id of the object in the map file.</param>
/// <param name="TypeId">Type id of the object in the map file (full name of the object description).</param>
/// <param name="Instance">Created object instance.</param>
public readonly record struct MapObjectEntry(int MapId, string TypeId, object Instance);

/// <summary>
/// Gives access to the objects created from the map, by their map ids. Registered in the game services.
/// </summary>
public interface IMapObjects
{
    IReadOnlyList<MapObjectEntry> All { get; }
    
    bool TryGet(int mapId, out MapObjectEntry entry);
}
