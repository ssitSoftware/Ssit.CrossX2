using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Ssit.CrossX2.Framework.Games.Template;

namespace Ssit.CrossX2.Editor.Models;

public interface IZoomData
{
    decimal Zoom { get; set; }
    void RequestSave();
}

public class EditorData: IZoomData, IDisposable
{
    public const int MaxRecentMaps = 10;

    [JsonProperty] public string RecentMapPath { get; set; }

    /// <summary>Folder the last map was opened from or saved to, kept apart from the prefabs' folder.</summary>
    [JsonProperty] public string MapFolder { get; set; }

    /// <summary>Recently opened or saved maps, newest first.</summary>
    [JsonProperty] public List<string> RecentMaps { get; set; } = new();

    /// <summary>Folder the last prefab was saved to, kept apart from the maps' folder.</summary>
    [JsonProperty] public string PrefabFolder { get; set; }

    /// <summary>Selection tool: a region selection also selects objects on the other layers with the same speed.</summary>
    [JsonProperty] public bool SelectObjectsOnAllLayers { get; set; }

    /// <summary>Region selection and Extract prefab take only objects fully inside the region (not all touching it).</summary>
    [JsonProperty] public bool SelectOnlyContainedObjects { get; set; }

    /// <summary>Extract prefab default: tiles only (true) or tiles and objects (false).</summary>
    [JsonProperty] public bool PrefabTilesOnly { get; set; }

    /// <summary>Extract prefab default: all layers with the same speed (true) or the current layer only (false).</summary>
    [JsonProperty] public bool PrefabAllMatchingLayers { get; set; }
    [JsonProperty] public bool ShowAllLayers { get; set; }
    [JsonProperty] public bool ShowGrid { get; set; } = true;
    [JsonProperty] public bool OnionMode { get; set; }
    [JsonProperty] public bool Animate { get; set; }
    [JsonProperty] public decimal Zoom { get; set; } = 1;
    [JsonProperty] public bool ShowLinks { get; set; }
    [JsonProperty] public bool ShowObjects { get; set; } = true;

    /// <summary>Name and position labels on selected objects.</summary>
    [JsonProperty] public bool ShowObjectLabels { get; set; } = true;
    [JsonProperty] public bool ShowMaterials { get; set; }
    [JsonProperty] public bool ShowCollisions { get; set; }
    [JsonProperty] public float CameraX { get; set; }
    [JsonProperty] public float CameraY { get; set; }
    [JsonProperty] public string SelectedLayer { get; set; } = LayerDescription.MainLayerId;

    private string _appName;

    private readonly Timer _timer;
    private bool _shouldSave;
    
    private object _lock = new();

    public EditorData()
    {
        _timer = new Timer(Save, null, 1000, 1000);
    }
    
    public static EditorData Load(string appName)
    {
        var path = GetFilePath(appName);

        if (!File.Exists(path))
            return new EditorData
            {
                _appName = appName
            };
        
        using var stream = File.Open(path, FileMode.Open);
        using var streamReader = new StreamReader(stream);
        
        var json = streamReader.ReadToEnd();
        
        var data = JsonConvert.DeserializeObject<EditorData>(json);
        data._appName = appName;
        return data;
    }

    /// <summary>Moves the map to the top of the recent list and remembers its folder.</summary>
    public void AddRecentMap(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        lock (_lock)
        {
            RecentMaps ??= new List<string>();
            RecentMaps.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
            RecentMaps.Insert(0, path);

            if (RecentMaps.Count > MaxRecentMaps)
            {
                RecentMaps.RemoveRange(MaxRecentMaps, RecentMaps.Count - MaxRecentMaps);
            }

            MapFolder = Path.GetDirectoryName(path);
            _shouldSave = true;
        }
    }

    public void RemoveRecentMap(string path)
    {
        lock (_lock)
        {
            RecentMaps?.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
            _shouldSave = true;
        }
    }

    public void ClearRecentMaps()
    {
        lock (_lock)
        {
            RecentMaps?.Clear();
            _shouldSave = true;
        }
    }

    /// <summary>Snapshot of the recent list, safe to enumerate while the timer saves.</summary>
    public string[] GetRecentMaps()
    {
        lock (_lock)
        {
            return RecentMaps?.ToArray() ?? [];
        }
    }

    public void RequestSave()
    {
        lock (_lock)
        {
            _shouldSave = true;
        }
    }

    private void Save(object _)
    {
        lock (_lock)
        {
            if (!_shouldSave)
                return;

            _shouldSave = false;

            var json = JsonConvert.SerializeObject(this);
            using var stream = File.Open(GetFilePath(_appName), FileMode.Create);
            using var streamWriter = new StreamWriter(stream);

            streamWriter.Write(json);
            streamWriter.Flush();
            streamWriter.Close();
            stream.Close();
        }
    }

    private static string GetFilePath(string appName)
    {
        var dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        dir = Path.Combine(dir, appName);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "data.json");
        return path;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}