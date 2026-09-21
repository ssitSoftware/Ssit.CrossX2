using Newtonsoft.Json;
using Ssit.CrossX2.Framework.IO;

namespace Ssit.CrossX2.Framework.Utils;

public abstract class AppSettingsBase<TSettings>: BindableModel where TSettings: AppSettingsBase<TSettings>, new()
{
    private IFileStorage _storage;
    private string _path;
    
    public static TSettings Load(IFileStorage storage, string path)
    {
        try
        {
            var json = storage.ReadText($"{path}.json");
            var data = JsonConvert.DeserializeObject<TSettings>(json);
            data._storage = storage;
            data._path = path;
            return data;
        }
        catch
        {
            return new TSettings
            {
                _storage = storage,
                _path = path
            };
        }
    }
    
    public void Save()
    {
        var json = JsonConvert.SerializeObject(this);
        _storage.WriteText($"{_path}.json", json);
    }
}