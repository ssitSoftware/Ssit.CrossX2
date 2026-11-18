namespace Ssit.CrossX2.Framework.IO;

/// <summary>
/// Reads and writes persistent application files.
/// </summary>
public interface IFileStorage
{
    string ReadText(string path);
    
    /// <summary>
    /// Writes the text file. Missing parent directories are created.
    /// </summary>
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

    /// <summary>
    /// Returns true if the directory exists.
    /// </summary>
    bool DirectoryExists(string path);

    /// <summary>
    /// Moves (renames) the directory with all its content. Fails if the destination already exists.
    /// </summary>
    void MoveDirectory(string sourcePath, string destinationPath);

    /// <summary>
    /// Deletes the directory with all its content. Does nothing if the directory doesn't exist.
    /// </summary>
    void DeleteDirectory(string path);

    /// <summary>
    /// Returns the names (without the directory) of the files in the directory, not of its subdirectories.
    /// Empty if the directory doesn't exist.
    /// </summary>
    string[] GetFiles(string path);

    /// <summary>
    /// Writes the binary file. Missing parent directories are created.
    /// </summary>
    void WriteData(string path, byte[] data);
    byte[] ReadData(string path);
    
    DateTime GetLastModificationTime(string path);
}
