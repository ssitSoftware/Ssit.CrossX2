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
    /// Saves the checkpoint (usually via <see cref="ICheckpointService"/>) and gives the player feedback.
    /// Called only when <see cref="CanCreateCheckpoint"/> is true.
    /// </summary>
    void CreateCheckpoint();
}
