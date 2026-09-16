namespace CrossX2.IO;

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

    public void WriteData(string path, byte[] data)
    {
        File.WriteAllBytes(Path.Combine(StorageDirectory(appName), path), data);
    }

    public byte[] ReadData(string path)
    {
        return File.ReadAllBytes(Path.Combine(StorageDirectory(appName), path));
    }

    private static string StorageDirectory(string appName)
    {
        var dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        dir = Path.Combine(dir, appName);
        Directory.CreateDirectory(dir);
        return dir;
    }
}