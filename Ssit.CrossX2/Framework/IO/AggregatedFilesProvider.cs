namespace Ssit.CrossX2.Framework.IO;

public class AggregatedFilesProvider: IFilesProvider
{
    private readonly Dictionary<string, IFilesProvider[]> _fileProviders = new();

    public AggregatedFilesProvider(AggregatedFilesProvider other = null)
    {
        if (other != null)
        {
            foreach (var kv in other._fileProviders)
            {
                _fileProviders.Add(kv.Key, kv.Value);
            }
        }
    }
    
    public AggregatedFilesProvider AddProvider(string drive, params IFilesProvider[] providers)
    {
        if (providers.Length == 0)
        {
            throw new InvalidOperationException("At least one provider must be specified");
        }

        _fileProviders.Add(drive, providers);
        return this;
    }
    
    public Stream Open(string path)
    {
        PathHelper.GetDriveAndPath(path, out var drive, out var newPath);

        if (!_fileProviders.TryGetValue(drive, out var providers))
        {
            throw new FileNotFoundException($"File {path} not found. Drive not found.");
        }

        foreach (var provider in providers)
        {
            if (provider.FileExists(newPath))
            {
                return provider.Open(newPath);
            }
        }
        
        throw new FileNotFoundException($"File {path} not found.");
    }

    public bool FileExists(string path)
    {
        PathHelper.GetDriveAndPath(path, out var drive, out var newPath);

        if (!_fileProviders.TryGetValue(drive, out var providers))
        {
            return false;
        }
        
        foreach (var provider in providers)
        {
            if (provider.FileExists(newPath))
            {
                return true;
            }
        }

        return false;
    }

    public string[] GetFiles(string path, string extension)
    {
        PathHelper.GetDriveAndPath(path, out var drive, out var newPath);
        
        if (!_fileProviders.TryGetValue(drive, out var providers))
        {
            return [];
        }

        var files = new List<string>();

        foreach (var provider in providers)
        {
            files.AddRange(provider.GetFiles(newPath, extension).Select(o => drive + '/' + o));
        }

        return files.ToArray();
    }

    public string GetPhisicalFilePath(string path)
    {
        PathHelper.GetDriveAndPath(path, out var drive, out var newPath);
        
        if (!_fileProviders.TryGetValue(drive, out var providers))
        {
            throw new FileNotFoundException($"File {path} not found. Drive not found.");
        }

        foreach (var provider in providers)
        {
            if (provider.FileExists(newPath))
            {
                return provider.GetPhisicalFilePath(newPath);
            }
        }
        
        return providers[0].GetPhisicalFilePath(newPath);
    }
}