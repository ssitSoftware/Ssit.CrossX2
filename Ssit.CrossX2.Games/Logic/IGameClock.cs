namespace Ssit.CrossX2.Framework.Games.Logic;

public interface IGameClock
{
    /// <summary>
    /// Game time (seconds) - the time passed for the character. Saved with checkpoints and restored from them
    /// (<see cref="Set"/>).
    /// </summary>
    double Time { get; }

    /// <summary>
    /// Total game time (seconds) of the save slot - how long the player has actually played the game in the slot,
    /// including the play lost by restoring a checkpoint. Not part of the save data, not restored from checkpoints:
    /// kept by <see cref="Checkpoints.GameInfoService"/> and stored in the GameInfo file of the slot directory.
    /// </summary>
    double TotalGameTime { get; }

    void Update(float dt);
    void Set(double time);
}