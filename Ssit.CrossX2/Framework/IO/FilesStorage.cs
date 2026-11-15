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
        File.WriteAllText(Path.Combine(StorageDirectory(appName), path), text);
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

    public void WriteData(string path, byte[] data)
    {
        File.WriteAllBytes(Path.Combine(StorageDirectory(appName), path), data);
    }

    public byte[] ReadData(string path)
    {
        return File.ReadAllBytes(Path.Combine(StorageDirectory(appName), path));
    }

    public DateTime GetLastModificationTime(string path)
    {
        return File.GetLastWriteTime(Path.Combine(StorageDirectory(appName), path));
    }

    private static string StorageDirectory(string appName)
    {
        var dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        dir = Path.Combine(dir, appName);
        Directory.CreateDirectory(dir);
        return dir;
    }
}
