namespace Ssit.CrossX2.Framework.Games.Logic.Checkpoints;

/// <summary>
/// Captures and restores the state of the map objects. Registered in the services of every game instance.
/// </summary>
public interface ICheckpointService
{
    /// <summary>
    /// Path of the map of the current game instance.
    /// </summary>
    string MapPath { get; }
    
    /// <summary>
    /// The last checkpoint captured or restored in the current game instance (null if none).
    /// </summary>
    CheckpointData LastCheckpoint { get; }
    
    /// <summary>
    /// Captures the current state of the map objects. Must be called from the game update.
    /// </summary>
    /// <param name="checkpointId">Map id of the checkpoint object that was reached.</param>
    CheckpointData Capture(int checkpointId);
    
    /// <summary>
    /// Captures the current state and writes it to the application storage as the last checkpoint.
    /// The file is written in the background.
    /// </summary>
    CheckpointData Save(int checkpointId);
    
    /// <summary>
    /// Restores the state of the map objects. Called by the game instance right after the map is loaded,
    /// when the instance was created with a checkpoint.
    /// </summary>
    void Restore(CheckpointData data);
}
