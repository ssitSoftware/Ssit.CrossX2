using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Ssit.CrossX2.Framework.IO;

namespace Ssit.CrossX2.Framework.Games.Logic.Checkpoints;

/// <summary>
/// Reads and writes checkpoints in the application storage. Each save slot has its own directory
/// <c>Slot{N}</c> (<see cref="SlotDirectory"/>) containing:
/// <list type="bullet">
/// <item><c>GameState</c> - <see cref="GameStateData"/>: map the game continues on, game state flags, player state;</item>
/// <item>one file per map, "{MapId}State" (<see cref="MapStateFileName"/>) - <see cref="MapStateData"/>;</item>
/// <item><c>Preview.jpg</c> - screen preview of the last checkpoint (<see cref="CheckpointData.Preview"/>).</item>
/// </list>
/// State files are stored as BSON or JSON (<see cref="Parameters.Binary"/>) with the matching extension.
/// Each state file is signed with HMAC-SHA256 (<see cref="Parameters.SigningKey"/>), so a file edited by hand is detected:
/// it is treated as non-existing and deleted.
/// Registered in the application services (one instance per game, with the game's own signing key).
/// </summary>
public class CheckpointStorage
{
    public class Parameters
    {
        /// <summary>
        /// Secret key used to sign checkpoints - each game has its own key.
        /// </summary>
        public byte[] SigningKey { get; init; }

        /// <summary>
        /// True: the checkpoint is stored as BSON (binary). False: as JSON (readable, e.g. for debugging).
        /// Either way it is signed, so it still can't be edited by hand.
        /// </summary>
        public bool Binary { get; init; }
    }

    private readonly Lock _lock = new();
    private readonly IFileStorage _storage;
    private readonly byte[] _signingKey;
    private readonly bool _useBinary;

    /// <summary>
    /// Stored BSON file: the checkpoint BSON kept as raw bytes and signed exactly as stored,
    /// so re-serialization differences can't break the signature.
    /// </summary>
    private class SignedBsonCheckpoint
    {
        public int Version { get; set; } = 1;
        public byte[] Payload { get; set; }
        public byte[] Signature { get; set; }
    }

    /// <summary>
    /// Stored JSON file: the checkpoint JSON kept as a string (signed as its UTF-8 bytes) and a Base64 signature.
    /// </summary>
    private class SignedJsonCheckpoint
    {
        public int Version { get; set; } = 1;
        public string Payload { get; set; }
        public string Signature { get; set; }
    }

    private static readonly JsonSerializer Serializer = JsonSerializer.CreateDefault();

    /// <summary>
    /// Name of the game state file in the slot directory (without extension).
    /// </summary>
    public const string GameStateFileName = "GameState";

    /// <summary>
    /// Name of the preview file in the slot directory. Plain JPEG, not signed - it is only displayed.
    /// </summary>
    public const string PreviewFileName = "Preview.jpg";

    private enum ReadResult
    {
        Valid,
        Corrupted,
        Unsupported
    }

    public CheckpointStorage(IFileStorage storage, Parameters parameters)
    {
        if (parameters.SigningKey is not { Length: > 0 })
            throw new ArgumentException("Checkpoint signing key is required.", nameof(parameters));
        
        _storage = storage;
        _signingKey = parameters.SigningKey;
        _useBinary = parameters.Binary;
    }

    /// <summary>
    /// Extension matching the stored format: ".bson" or ".json". Added to the file names.
    /// </summary>
    private string FileExtension => _useBinary ? ".bson" : ".json";

    /// <summary>
    /// Directory of the save slot in the application storage.
    /// </summary>
    public static string SlotDirectory(int slot) => $"Slot{slot}";

    /// <summary>
    /// Name of the map state file in the slot directory (without extension), e.g. "Map01" → "Map01State".
    /// </summary>
    /// <param name="mapId">Map id (<see cref="Map.MapPathHelper.MapId"/>).</param>
    public static string MapStateFileName(string mapId) => $"{mapId}State";

    private string FilePath(int slot, string fileName) => Path.Combine(SlotDirectory(slot), fileName + FileExtension);
    
    /// <summary>
    /// Writes the checkpoint into the slot directory: the map state into the file of its map (if the checkpoint has one),
    /// then the game state and the preview. States of other maps stored in the slot are kept.
    /// The map state goes into the file of <see cref="MapStateData.MapId"/> - when the game continues on another map
    /// (the checkpoint was saved when leaving the map), it is not the map of the game state.
    /// A checkpoint without a preview removes the previous one, so it never shows an older save.
    /// </summary>
    public void Write(int slot, CheckpointData data)
    {
        var mapStateId = data.MapState?.MapId ?? data.MapId;
        
        if (data.MapState is not null && string.IsNullOrEmpty(mapStateId))
            throw new ArgumentException("Checkpoint with a map state needs the map id.", nameof(data));
        
        var mapStateBytes = data.MapState is null ? null : Serialize(data.MapState);
        var gameStateBytes = Serialize(data.GameState);

        lock (_lock)
        {
            if (mapStateBytes is not null)
            {
                _storage.WriteData(FilePath(slot, MapStateFileName(mapStateId)), mapStateBytes);
            }
            
            _storage.WriteData(FilePath(slot, GameStateFileName), gameStateBytes);

            var previewPath = Path.Combine(SlotDirectory(slot), PreviewFileName);
            
            if (data.Preview is { Length: > 0 })
            {
                _storage.WriteData(previewPath, data.Preview);
            }
            else if (_storage.Exists(previewPath))
            {
                _storage.Delete(previewPath);
            }
        }
    }

    /// <summary>
    /// Moves the slot directory aside to "Slot{N}_arch{M}" with the first free M from 0.
    /// Does nothing if the slot has no directory.
    /// </summary>
    /// <returns>Name of the archived directory, or null if there was nothing to archive.</returns>
    public string Archive(int slot)
    {
        lock (_lock)
        {
            var source = SlotDirectory(slot);
            if (!_storage.DirectoryExists(source))
                return null;

            for (var index = 0; ; index++)
            {
                var destination = $"{source}_arch{index}";
                if (_storage.DirectoryExists(destination))
                    continue;

                _storage.MoveDirectory(source, destination);
                return destination;
            }
        }
    }

    /// <summary>
    /// Deletes the slot directory with all its checkpoint files, if any.
    /// </summary>
    public void Delete(int slot)
    {
        lock (_lock)
        {
            _storage.DeleteDirectory(SlotDirectory(slot));
        }
    }

    /// <summary>
    /// Reads the game state of the slot and the state of the map it was saved on.
    /// Returns null if the slot has no usable game state. <see cref="CheckpointData.MapState"/> is null
    /// if the map has no usable state.
    /// </summary>
    public CheckpointData Read(int slot)
    {
        var gameState = ReadGameState(slot);
        if (gameState is null)
            return null;
        
        var mapState = string.IsNullOrEmpty(gameState.MapId) ? null : ReadMapState(slot, gameState.MapId, gameState.MapPath);
        return new CheckpointData(gameState, mapState);
    }

    /// <summary>
    /// Reads the game state of the slot. Returns null if there is none or it can't be used.
    /// </summary>
    public GameStateData ReadGameState(int slot) =>
        ReadFile<GameStateData>(FilePath(slot, GameStateFileName), GameStateData.CurrentVersion);

    /// <summary>
    /// Reads the JPEG preview of the last checkpoint stored in the slot. Returns null if there is none.
    /// </summary>
    public byte[] ReadPreview(int slot)
    {
        lock (_lock)
        {
            try
            {
                return _storage.ReadData(Path.Combine(SlotDirectory(slot), PreviewFileName));
            }
            catch
            {
                // No preview saved (or can't be read at the moment).
                return null;
            }
        }
    }

    /// <summary>
    /// Reads the state of the map stored in the slot. Returns null if there is none or it can't be used.
    /// </summary>
    /// <param name="slot">Save slot.</param>
    /// <param name="mapId">Map id (<see cref="Map.MapPathHelper.MapId"/>) naming the state file.</param>
    /// <param name="mapPath">Path of the map - the stored state is used only if it belongs to this map.</param>
    public MapStateData ReadMapState(int slot, string mapId, string mapPath)
    {
        var data = ReadFile<MapStateData>(FilePath(slot, MapStateFileName(mapId)), MapStateData.CurrentVersion);
        
        // Different map paths can give the same map id - use only the state of the requested map.
        return data is not null && string.Equals(data.MapPath, mapPath, StringComparison.Ordinal) ? data : null;
    }

    /// <summary>
    /// Reads a checkpoint file. Returns null if it doesn't exist or can't be used.
    /// A corrupted file (changed by hand, wrong signature, unreadable content) is deleted.
    /// </summary>
    /// <param name="fileName">Path in the application storage, with extension.</param>
    /// <param name="supportedVersion">Newest file version this game can read.</param>
    private T ReadFile<T>(string fileName, int supportedVersion) where T : class, ICheckpointFile
    {
        lock (_lock)
        {
            byte[] bytes;
            try
            {
                bytes = _storage.ReadData(fileName);
            }
            catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
            {
                // No checkpoint saved yet.
                return null;
            }
            catch
            {
                // Can't be read at the moment (e.g. locked) - not proven corrupted, keep it.
                return null;
            }

            var result = Verify<T>(bytes, supportedVersion, out var data);

            if (result == ReadResult.Corrupted)
            {
                Console.WriteLine($"Checkpoint file '{fileName}' is corrupted - deleting it.");

                try
                {
                    _storage.Delete(fileName);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to delete corrupted checkpoint: {e}");
                }
            }

            return result == ReadResult.Valid ? data : null;
        }
    }

    private ReadResult Verify<T>(byte[] bytes, int supportedVersion, out T data) where T : class, ICheckpointFile
    {
        data = null;
        
        try
        {
            var (payload, signature) = _useBinary ? UnwrapBson(bytes) : UnwrapJson(bytes);
            if (payload is null || signature is null)
                return ReadResult.Corrupted;

            var expected = Sign(payload);
            if (!CryptographicOperations.FixedTimeEquals(expected, signature))
                return ReadResult.Corrupted;

            data = _useBinary
                ? FromBson<T>(payload)
                : JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(payload));
            if (data is null || string.IsNullOrEmpty(data.MapPath))
                return ReadResult.Corrupted;

            // Saved by a newer version of the game - valid, but can't be used by this one. Don't delete it.
            if (data.Version > supportedVersion)
                return ReadResult.Unsupported;

            return ReadResult.Valid;
        }
        catch
        {
            // Not BSON/JSON, not Base64, wrong shape...
            return ReadResult.Corrupted;
        }
    }

    private byte[] Sign(byte[] payload) => HMACSHA256.HashData(_signingKey, payload);

    private byte[] Serialize(object data) => _useBinary ? SerializeBson(data) : SerializeJson(data);

    private byte[] SerializeBson(object data)
    {
        var payload = ToBson(data);
        
        return ToBson(new SignedBsonCheckpoint
        {
            Payload = payload,
            Signature = Sign(payload)
        });
    }

    private byte[] SerializeJson(object data)
    {
        var payload = JsonConvert.SerializeObject(data);
        
        var signed = new SignedJsonCheckpoint
        {
            Payload = payload,
            Signature = Convert.ToBase64String(Sign(Encoding.UTF8.GetBytes(payload)))
        };

        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(signed, Formatting.Indented));
    }

    private static (byte[] Payload, byte[] Signature) UnwrapBson(byte[] bytes)
    {
        var signed = FromBson<SignedBsonCheckpoint>(bytes);
        return (signed?.Payload, signed?.Signature);
    }

    private static (byte[] Payload, byte[] Signature) UnwrapJson(byte[] bytes)
    {
        var signed = JsonConvert.DeserializeObject<SignedJsonCheckpoint>(Encoding.UTF8.GetString(bytes));
        
        return (
            signed?.Payload is null ? null : Encoding.UTF8.GetBytes(signed.Payload),
            signed?.Signature is null ? null : Convert.FromBase64String(signed.Signature));
    }

    private static byte[] ToBson<T>(T value)
    {
        using var stream = new MemoryStream();
        
        using (var writer = new BsonDataWriter(stream))
        {
            Serializer.Serialize(writer, value);
        }

        return stream.ToArray();
    }

    private static T FromBson<T>(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var reader = new BsonDataReader(stream) { DateTimeKindHandling = DateTimeKind.Utc };
        
        return Serializer.Deserialize<T>(reader);
    }
}
