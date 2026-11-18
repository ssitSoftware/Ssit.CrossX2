namespace Ssit.CrossX2.Framework.IO;

/// <summary>
/// Stores application data files under the user's local application data directory.
/// </summary>
public class FilesStorage(string appName) : IFileStorage
{
    public string ReadText(string path)
    {
        return File.ReadAllText(Path.Combine(StorageDirectory(appName), path));
    }

    public void WriteText(string path, string text)
    {
        File.WriteAllText(PrepareWritePath(path), text);
    }

    public void Delete(string path)
    {
        File.Delete(Path.Combine(StorageDirectory(appName), path));
    }

    public bool Exists(string path)
    {
        return File.Exists(Path.Combine(StorageDirectory(appName), path));
    }

    public void Move(string sourcePath, string destinationPath)
    {
        var directory = StorageDirectory(appName);
        File.Move(Path.Combine(directory, sourcePath), Path.Combine(directory, destinationPath));
    }

    public bool DirectoryExists(string path)
    {
        return Directory.Exists(Path.Combine(StorageDirectory(appName), path));
    }

    public void MoveDirectory(string sourcePath, string destinationPath)
    {
        var directory = StorageDirectory(appName);
        Directory.Move(Path.Combine(directory, sourcePath), Path.Combine(directory, destinationPath));
    }

    public void DeleteDirectory(string path)
    {
        var fullPath = Path.Combine(StorageDirectory(appName), path);
        
        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, true);
        }
    }

    public string[] GetFiles(string path)
    {
        var fullPath = Path.Combine(StorageDirectory(appName), path);

        if (!Directory.Exists(fullPath))
        {
            return [];
        }

        return Directory.GetFiles(fullPath).Select(Path.GetFileName).ToArray();
    }

    public void WriteData(string path, byte[] data)
    {
        File.WriteAllBytes(PrepareWritePath(path), data);
    }

    public byte[] ReadData(string path)
    {
        return File.ReadAllBytes(Path.Combine(StorageDirectory(appName), path));
    }

    public DateTime GetLastModificationTime(string path)
    {
        return File.GetLastWriteTime(Path.Combine(StorageDirectory(appName), path));
    }

    /// <summary>
    /// Returns the full path of the file and creates its directory (files can be stored in subdirectories).
    /// </summary>
    private string PrepareWritePath(string path)
    {
        var fullPath = Path.Combine(StorageDirectory(appName), path);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return fullPath;
    }

    private static string StorageDirectory(string appName)
    {
        var dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        dir = Path.Combine(dir, appName);
        Directory.CreateDirectory(dir);
        return dir;
    }
}
