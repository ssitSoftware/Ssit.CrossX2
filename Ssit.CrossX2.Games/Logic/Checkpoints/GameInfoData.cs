namespace Ssit.CrossX2.Framework.Games.Logic.Checkpoints;

/// <summary>
/// Information about the game played in a save slot ("GameInfo.json" of the slot directory,
/// <see cref="CheckpointStorage.GameInfoFileName"/>). Not part of the save data - it isn't restored with checkpoints
/// (see <see cref="GameInfoService"/>).
/// </summary>
public class GameInfoData
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    /// <summary>
    /// Total game time (seconds) of the slot - how long the player has actually played the game in the slot
    /// (<see cref="IGameClock.TotalGameTime"/>).
    /// </summary>
    public double TotalGameTime { get; set; }
}
