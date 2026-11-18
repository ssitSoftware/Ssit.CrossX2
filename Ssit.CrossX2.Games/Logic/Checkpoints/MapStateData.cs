using Newtonsoft.Json.Linq;

namespace Ssit.CrossX2.Framework.Games.Logic.Checkpoints;

/// <summary>
/// Stored state of the map objects at a checkpoint. Each map has its own file in the slot directory, named after
/// the map id (<see cref="CheckpointStorage.MapStateFileName"/>).
/// </summary>
public class MapStateData : ICheckpointFile
{
    public const int CurrentVersion = 1;
    
    public int Version { get; set; } = CurrentVersion;
    
    /// <summary>
    /// Path of the map the state belongs to.
    /// </summary>
    public string MapPath { get; set; }

    /// <summary>
    /// Id of the map the state belongs to (<see cref="Map.MapPathHelper.MapId"/>) - names the state file in the slot
    /// directory. Can differ from <see cref="GameStateData.MapId"/> when the game continues on another map
    /// (the checkpoint was saved when leaving the map, see <see cref="ICheckpointService.Save"/>).
    /// Null in states saved before it was added - <see cref="CheckpointData.MapId"/> is used then.
    /// </summary>
    public string MapId { get; set; }
    
    public DateTime SavedAtUtc { get; set; }
    
    /// <summary>
    /// States of map objects by their map ids.
    /// </summary>
    public Dictionary<int, ObjectSnapshot> Objects { get; set; } = new();

    /// <summary>
    /// States of objects created at runtime (not from the map file), e.g. dropped collectibles.
    /// </summary>
    public List<SpawnedObjectSnapshot> SpawnedObjects { get; set; } = new();
}

/// <summary>
/// Stored state of a single map object.
/// </summary>
public class ObjectSnapshot
{
    /// <summary>
    /// Type id of the object - the snapshot is ignored if the map object with the same id has a different type
    /// (e.g. the map was edited after the checkpoint was saved).
    /// </summary>
    public string TypeId { get; set; }
    
    /// <summary>
    /// True if the object had been removed from the simulation (collected, destroyed, ...).
    /// </summary>
    public bool Removed { get; set; }
    
    /// <summary>
    /// Object state (<see cref="IStatefulObject{TState}"/>), null if the object doesn't store a state.
    /// </summary>
    public JToken State { get; set; }
}

/// <summary>
/// Stored state of an object created at runtime. It has no map id - it is recreated from its type when restored:
/// the type needs a public constructor taking its state object (other parameters are resolved from the game services),
/// <see cref="IStatefulObject.RestoreState"/> is called afterwards.
/// </summary>
public class SpawnedObjectSnapshot
{
    /// <summary>
    /// Type of the object: "Full.Type.Name, AssemblyName".
    /// </summary>
    public string ObjectType { get; set; }
    
    /// <summary>
    /// Type of the state object: "Full.Type.Name, AssemblyName".
    /// </summary>
    public string StateType { get; set; }
    
    public JToken State { get; set; }
}
