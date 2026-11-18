namespace Ssit.CrossX2.Framework.Games.Logic.Checkpoints;

/// <summary>
/// Captures and restores the state of the map objects, the game state and the player state.
/// Registered in the services of every game instance.
/// </summary>
public interface ICheckpointService
{
    /// <summary>
    /// Path of the map of the current game instance.
    /// </summary>
    string MapPath { get; }

    /// <summary>
    /// Id of the map of the current game instance - names its state file in the slot directory
    /// (<see cref="Map.MapPathHelper.MapId"/>).
    /// </summary>
    string MapId { get; }
    
    /// <summary>
    /// The last checkpoint captured or restored in the current game instance (null if none).
    /// </summary>
    CheckpointData LastCheckpoint { get; }
    
    /// <summary>
    /// Captures the current state of the map objects and the game state. Must be called from the game update.
    /// </summary>
    /// <param name="playerState">
    /// Player state stored in the game state (see <see cref="IPlayerStateProvider{TState}"/>), null if none.
    /// It is serialized immediately, so the caller may keep changing the object.
    /// </param>
    /// <param name="targetMapPath">
    /// Path of the map the game continues on (e.g. the player leaves the map through a map link), null for the current map.
    /// The map state is still the state of the current map (<see cref="MapStateData.MapId"/>), the game state
    /// (<see cref="CheckpointData.MapPath"/>, <see cref="CheckpointData.MapId"/>) points to the target map.
    /// </param>
    CheckpointData Capture(object playerState, string targetMapPath = null);
    
    /// <summary>
    /// Captures the current state and writes it to the directory of the save <paramref name="slot"/> in the application
    /// storage: the map state into the file of the current map, the game state with <paramref name="playerState"/>
    /// into the GameState file (see <see cref="CheckpointStorage"/>). The files are written in the background.
    /// With <paramref name="targetMapPath"/> the game state points to the target map, so the game continues there
    /// (see <see cref="Capture"/>).
    /// </summary>
    CheckpointData Save(int slot, object playerState, string targetMapPath = null);
    
    /// <summary>
    /// Restores the game time (<see cref="IGameClock.Set"/>, first - map objects can rely on it), the state of
    /// the map objects, the game state flags and the player state
    /// (<see cref="IPlayerStateProvider{TState}"/>), then calls <see cref="IStateRestoredHandler.OnStateRestored"/>.
    /// Called by the game instance right after the map is loaded, when the instance was created with a checkpoint.
    /// </summary>
    void Restore(CheckpointData data);
}
