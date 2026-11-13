using Newtonsoft.Json.Linq;

namespace Ssit.CrossX2.Framework.Games.Logic.Checkpoints;

/// <summary>
/// Stored state of the game at a checkpoint.
/// </summary>
public class CheckpointData
{
    public const int CurrentVersion = 1;
    
    public int Version { get; set; } = CurrentVersion;
    
    /// <summary>
    /// Path of the map the checkpoint belongs to. The map is loaded from this path when the checkpoint is restored.
    /// </summary>
    public string MapPath { get; set; }
    
    /// <summary>
    /// Map id of the checkpoint object that was reached (0 if the checkpoint was not saved by an object).
    /// </summary>
    public int CheckpointId { get; set; }
    
    public DateTime SavedAtUtc { get; set; }

    /// <summary>
    /// Game state flags (<see cref="IGameState"/>).
    /// </summary>
    public List<string> Flags { get; set; } = new();
    
    /// <summary>
    /// States of map objects by their map ids.
    /// </summary>
    public Dictionary<int, ObjectSnapshot> Objects { get; set; } = new();
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
