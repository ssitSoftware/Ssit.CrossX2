using Newtonsoft.Json;
using Ssit.CrossX2.Framework.IO;

namespace Ssit.CrossX2.Framework.Games.Logic.Checkpoints;

/// <summary>
/// Reads and writes the last checkpoint in the application storage.
/// </summary>
public static class CheckpointStorage
{
    public const string LastCheckpointFileName = "checkpoint.json";

    private static readonly Lock WriteLock = new();
    
    public static void Write(IFileStorage storage, CheckpointData data, string fileName = LastCheckpointFileName)
    {
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);

        lock (WriteLock)
        {
            storage.WriteText(fileName, json);
        }
    }

    /// <summary>
    /// Reads the stored checkpoint. Returns null if there is no checkpoint or it can't be read.
    /// </summary>
    public static CheckpointData Read(IFileStorage storage, string fileName = LastCheckpointFileName)
    {
        try
        {
            string json;
            lock (WriteLock)
            {
                json = storage.ReadText(fileName);
            }
            
            var data = JsonConvert.DeserializeObject<CheckpointData>(json);

            if (data is null || string.IsNullOrEmpty(data.MapPath) || data.Version > CheckpointData.CurrentVersion)
                return null;

            return data;
        }
        catch
        {
            // No checkpoint saved yet or the file is corrupted.
            return null;
        }
    }
}
