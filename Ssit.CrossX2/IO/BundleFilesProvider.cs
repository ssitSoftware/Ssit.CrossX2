namespace Ssit.CrossX2.IO;

public class LocalFilesProvider : IFilesProvider
{
    public Stream Open(string path)
    {
        path = FixPath(path);
        return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public bool FileExists(string path)
    {
        path = FixPath(path);
        path = PathHelper.NormalizePath(path);
        return File.Exists(path);
    }

    public string[] GetFiles(string path, string extension = null)
    {
        return Directory.GetFiles(path, $"*.{extension ?? "*"}");
    }

    private string FixPath(string path)
    {
        if (!path.StartsWith("/")) path = "/" + path;
        return path;
    }

    public string GetPhisicalFilePath(string path)
    {
        return path;
    }
}

public class BundleFilesProvider: IFilesProvider
{
    private readonly string _bundleDir = "";
    public BundleFilesProvider()
    {
        var location = AppDomain.CurrentDomain.BaseDirectory;
        var dir = Path.GetDirectoryName(location) ?? string.Empty;
        
        Console.WriteLine($"Location: {dir}");
        
        string[] dirs = [
            dir + "/Resources/",
            dir + "/../Resources/",
            dir
        ];

        foreach (var directory in dirs)
        {
            var testDir = PathHelper.NormalizePath(directory);
            if (Directory.Exists(testDir))
            {
                _bundleDir = testDir;
                break;
            }
        }
        
        Console.WriteLine($"BundleDir: {_bundleDir}");
    }
    
    public Stream Open(string path)
    {
        var resPath = GetFullPath(path).Replace("!", "");;
        return File.Open(resPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public bool FileExists(string path)
    {
        path = GetFullPath(path).Replace("!", "");
        return File.Exists(path);
    }

    public string[] GetFiles(string path, string extension = null)
    {
        return [];
    }

    public string GetPhisicalFilePath(string path)
    {
        return GetFullPath(path).Replace("!", "");
    }

    private string GetFullPath(string path)
    {
        path = PathHelper.NormalizePath(path);
        path = Path.Combine(_bundleDir, path).Replace("!", "");

        return path;
    }
}