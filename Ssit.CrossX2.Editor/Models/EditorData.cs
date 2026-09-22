using System;
using System.IO;
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
    [JsonProperty] public string RecentMapPath { get; set; }
    [JsonProperty] public bool ShowAllLayers { get; set; }
    [JsonProperty] public bool ShowGrid { get; set; } = true;
    [JsonProperty] public bool OnionMode { get; set; }
    [JsonProperty] public bool Animate { get; set; }
    [JsonProperty] public decimal Zoom { get; set; } = 1;
    [JsonProperty] public bool ShowLinks { get; set; }
    [JsonProperty] public bool ShowObjects { get; set; } = true;
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