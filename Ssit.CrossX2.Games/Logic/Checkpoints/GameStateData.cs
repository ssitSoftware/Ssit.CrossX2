using Newtonsoft.Json.Linq;

namespace Ssit.CrossX2.Framework.Games.Logic.Checkpoints;

/// <summary>
/// Stored state of the game in a save slot ("GameState" file of the slot directory): the map the game continues on,
/// game time, game state flags and the player state. States of the map objects are stored per map (<see cref="MapStateData"/>),
/// the screen preview in a separate JPEG file (<see cref="CheckpointStorage.PreviewFileName"/>).
/// </summary>
public class GameStateData : ICheckpointFile
{
    public const int CurrentVersion = 1;
    
    public int Version { get; set; } = CurrentVersion;
    
    /// <summary>
    /// Path of the map the game was saved on. The map is loaded from this path when the game is continued.
    /// </summary>
    public string MapPath { get; set; }

    /// <summary>
    /// Id of the map the game was saved on - names its state file in the slot directory
    /// (<see cref="CheckpointStorage.MapStateFileName"/>).
    /// </summary>
    public string MapId { get; set; }
    
    public DateTime SavedAtUtc { get; set; }

    /// <summary>
    /// Game time (<see cref="IGameClock.Time"/>) at the moment the checkpoint was saved.
    /// </summary>
    public double Time { get; set; }

    /// <summary>
    /// Game state flags (<see cref="IGameState"/>).
    /// </summary>
    public List<string> Flags { get; set; } = new();

    /// <summary>
    /// Player state passed to <see cref="ICheckpointService.Save"/> and restored through
    /// <see cref="IPlayerStateProvider{TState}"/>. Null if no player state was passed.
    /// </summary>
    public JToken PlayerState { get; set; }
}
