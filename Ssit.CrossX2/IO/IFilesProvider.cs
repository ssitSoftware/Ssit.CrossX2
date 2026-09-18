namespace Ssit.CrossX2.IO;

/// <summary>
/// Provides methods to open files from various locations.
/// </summary>
public interface IFilesProvider
{
    /// <summary>
    /// Opens a file stream for the given path.
    /// </summary>
    /// <param name="path">The path of the file to open.</param>
    /// <returns>A stream to read the file.</returns>
    Stream Open(string path);

    /// <summary>
    /// Checks if a file exists at the specified path.
    /// </summary>
    /// <param name="path">The path of the file to check.</param>
    /// <returns>True if the file exists; otherwise, false.</returns>
    bool FileExists(string path);
    
    string[] GetFiles(string path, string extension = null);

    /// <summary>
    /// Retrieves the physical file path for the specified virtual path.
    /// </summary>
    /// <param name="path">The virtual path of the file.</param>
    /// <returns>The physical file path corresponding to the provided virtual path.</returns>
    string GetPhisicalFilePath(string path);
}