namespace Ssit.CrossX2.Framework.Games.Logic.Checkpoints;

/// <summary>
/// Object that can save a checkpoint (see <see cref="Platformer.Behaviors.SteeringCharacters.CreateCheckpointBehavior"/>).
/// </summary>
public interface ICheckpointCreator
{
    /// <summary>
    /// False when a checkpoint can't be saved at the moment (e.g. during a fight).
    /// </summary>
    bool CanCreateCheckpoint { get; }

    /// <summary>
    /// True when the creator is at an existing checkpoint - holding the save button for half of the hold time
    /// (the time needed to start creating a new checkpoint) saves there, without entering the creating state.
    /// </summary>
    bool IsAtCheckpoint => false;
    
    /// <summary>
    /// Saves the checkpoint (usually via <see cref="ICheckpointService"/>) and gives the player feedback.
    /// Called only when <see cref="CanCreateCheckpoint"/> is true.
    /// </summary>
    void CreateCheckpoint();

    /// <summary>
    /// Called when the character enters the creating state (the save button has been held for half of the hold time).
    /// </summary>
    void BeginCreateCheckpoint()
    {
    }

    /// <summary>
    /// Called when creating was interrupted before the checkpoint was saved
    /// (button released or <see cref="CanCreateCheckpoint"/> became false).
    /// </summary>
    void CancelCreateCheckpoint()
    {
    }
}
