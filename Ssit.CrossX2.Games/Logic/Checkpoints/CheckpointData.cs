namespace Ssit.CrossX2.Framework.Games.Logic.Checkpoints;

/// <summary>
/// Checkpoint of a game: the game state of the slot and the state of the map the game was saved on.
/// Not stored as a whole - <see cref="CheckpointStorage"/> writes its parts into separate files of the slot directory.
/// </summary>
public class CheckpointData
{
    public CheckpointData(GameStateData gameState, MapStateData mapState, byte[] preview = null)
    {
        GameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
        MapState = mapState;
        Preview = preview;
    }

    /// <summary>
    /// Map the game continues on, game state flags and the player state.
    /// </summary>
    public GameStateData GameState { get; }

    /// <summary>
    /// States of the objects of the map, null if the map has no stored state (e.g. it hasn't been saved on yet).
    /// </summary>
    public MapStateData MapState { get; }

    /// <summary>
    /// JPEG preview of the screen at the moment the checkpoint was saved, stored as "Preview.jpg" in the slot directory.
    /// Null if previews are disabled (<c>AabbGameInstance.Parameters.StoreCheckpointPreview</c>) or it couldn't be taken.
    /// Not loaded by <see cref="CheckpointStorage.Read"/> - use <see cref="CheckpointStorage.ReadPreview"/>.
    /// </summary>
    public byte[] Preview { get; }

    /// <summary>
    /// Path of the map the checkpoint belongs to. The map is loaded from this path when the checkpoint is restored.
    /// </summary>
    public string MapPath => GameState.MapPath;

    /// <summary>
    /// Id of the map the checkpoint belongs to (<see cref="Map.MapPathHelper.MapId"/>).
    /// </summary>
    public string MapId => GameState.MapId;
}
