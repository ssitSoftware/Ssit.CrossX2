using System.Reflection;

namespace CrossX2.IO;

public class CacheFilesProvider: IFilesProvider
{
    private readonly string _dir;

    public CacheFilesProvider(string name, IFilesProvider originalProvider, string subdir = "")
    {
        var files = originalProvider.GetFiles(subdir);
        
        _dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), name);
        Directory.CreateDirectory(_dir);

        foreach (var file in files)
        {
            var fname = Path.GetFileName(file);
            var targetFile = Path.Combine(_dir, fname);

            if (File.Exists(targetFile))
            {
                var time = File.GetLastWriteTime(file);
                var targetTime = File.GetLastWriteTime(targetFile);

                if (time <= targetTime) continue;
            }

            using var stream = originalProvider.Open(file);
            using var fileStream = File.OpenWrite(targetFile);

            stream.CopyTo(fileStream);
            stream.Flush();
            fileStream.Flush();
        }
    }
    
    public CacheFilesProvider(Assembly assembly, string dir, string name): this(name, new EmbeddedFilesProvider(assembly, dir ?? assembly.GetName().Name))
    {
    }
    
    public Stream Open(string path)
    {
        return File.Open(Path.Combine(_dir, path).Replace("!", ""), FileMode.Open, FileAccess.Read);
    }

    public bool FileExists(string path)
    {
        return File.Exists(Path.Combine(_dir, path).Replace("!", ""));
    }

    public string[] GetFiles(string path, string extension = null)
    {
        var ext = extension ?? "*";
        return Directory.GetFiles(Path.Combine(_dir, path).Replace("!", ""), $"*.{ext}");
    }

    public string GetPhisicalFilePath(string path)
    {
        return Path.Combine(_dir, path).Replace("!", "");
    }
}