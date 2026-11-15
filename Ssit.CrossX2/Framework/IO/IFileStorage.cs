namespace Ssit.CrossX2.Framework.IO;

/// <summary>
/// Reads and writes persistent application files.
/// </summary>
public interface IFileStorage
{
    string ReadText(string path);
    void WriteText(string path, string text);

    /// <summary>
    /// Deletes the file. Does nothing if the file doesn't exist.
    /// </summary>
    void Delete(string path);

    /// <summary>
    /// Returns true if the file exists.
    /// </summary>
    bool Exists(string path);

    /// <summary>
    /// Moves (renames) the file. Fails if the destination already exists.
    /// </summary>
    void Move(string sourcePath, string destinationPath);

    void WriteData(string path, byte[] data);
    byte[] ReadData(string path);
    
    DateTime GetLastModificationTime(string path);
}
