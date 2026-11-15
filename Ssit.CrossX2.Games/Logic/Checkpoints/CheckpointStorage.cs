using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Ssit.CrossX2.Framework.IO;

namespace Ssit.CrossX2.Framework.Games.Logic.Checkpoints;

/// <summary>
/// Reads and writes the last checkpoint in the application storage, as BSON or JSON (<see cref="Parameters.UseBson"/>).
/// The checkpoint is signed with HMAC-SHA256 (<see cref="Parameters.SigningKey"/>), so a file edited by hand is detected:
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
        public bool UseBson { get; init; } = true;
    }

    private readonly Lock _lock = new();
    private readonly IFileStorage _storage;
    private readonly byte[] _signingKey;
    private readonly bool _useBson;

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
        _useBson = parameters.UseBson;
    }
    
    /// <summary>
    /// Writes the checkpoint to <paramref name="fileName"/> in the application storage.
    /// </summary>
    public void Write(string fileName, CheckpointData data)
    {
        var bytes = _useBson ? SerializeBson(data) : SerializeJson(data);

        lock (_lock)
        {
            _storage.WriteData(fileName, bytes);
        }
    }

    /// <summary>
    /// Deletes the checkpoint stored in <paramref name="fileName"/> (if any).
    /// </summary>
    public void Delete(string fileName)
    {
        lock (_lock)
        {
            _storage.Delete(fileName);
        }
    }

    /// <summary>
    /// Reads the checkpoint stored in <paramref name="fileName"/>. Returns null if there is no checkpoint or it can't be used.
    /// A corrupted checkpoint (changed by hand, wrong signature, unreadable content) is deleted.
    /// </summary>
    public CheckpointData Read(string fileName)
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

            var result = Verify(bytes, out var data);

            if (result == ReadResult.Corrupted)
            {
                Console.WriteLine("Checkpoint is corrupted - deleting it.");

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

    private ReadResult Verify(byte[] bytes, out CheckpointData data)
    {
        data = null;
        
        try
        {
            var (payload, signature) = _useBson ? UnwrapBson(bytes) : UnwrapJson(bytes);
            if (payload is null || signature is null)
                return ReadResult.Corrupted;

            var expected = Sign(payload);
            if (!CryptographicOperations.FixedTimeEquals(expected, signature))
                return ReadResult.Corrupted;

            data = _useBson
                ? FromBson<CheckpointData>(payload)
                : JsonConvert.DeserializeObject<CheckpointData>(Encoding.UTF8.GetString(payload));
            if (data is null || string.IsNullOrEmpty(data.MapPath))
                return ReadResult.Corrupted;

            // Saved by a newer version of the game - valid, but can't be used by this one. Don't delete it.
            if (data.Version > CheckpointData.CurrentVersion)
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

    private byte[] SerializeBson(CheckpointData data)
    {
        var payload = ToBson(data);
        
        return ToBson(new SignedBsonCheckpoint
        {
            Payload = payload,
            Signature = Sign(payload)
        });
    }

    private byte[] SerializeJson(CheckpointData data)
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
