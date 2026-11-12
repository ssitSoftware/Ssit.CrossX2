namespace Ssit.CrossX2.Framework.IO;

/// <summary>
/// Reads and writes persistent application files.
/// </summary>
public interface IFileStorage
{
    string ReadText(string path);
    void WriteText(string path, string text);

    void WriteData(string path, byte[] data);
    byte[] ReadData(string path);
    
    DateTime GetLastModificationTime(string path);
}
