namespace Ssit.CrossX2.Framework.Games.Logic.Checkpoints;

/// <summary>
/// Object that can save a checkpoint (see <see cref="Platformer.Behaviors.SteeringCharacters.CreateCheckpointBehavior"/>).
/// </summary>
public interface ICheckpointCreator
{
    ICheckpointService CheckpointService { get; }
    
    /// <summary>
    /// False when a checkpoint can't be saved at the moment (e.g. during a fight).
    /// </summary>
    bool CanCreateCheckpoint { get; }
    
    /// <summary>
    /// Called after the checkpoint was saved.
    /// </summary>
    void OnCheckpointCreated();
}
