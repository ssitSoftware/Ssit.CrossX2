namespace Ssit.CrossX2.Framework.Audio;

/// <summary>
/// Represents a song with a name and a file path.
/// </summary>
public class Song
{
    /// <summary>
    /// Gets the name of the song.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string Name { get; }

    /// <summary>
    /// Gets the file path of the song.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Constructs a song with a specific file path.
    /// </summary>
    public Song(string path)
    {
        Name = System.IO.Path.GetFileNameWithoutExtension(path);
        Path = path;
    }

    /// <summary>
    /// Constructs a song with a name and file path.
    /// </summary>
    public Song((string name, string path) song)
    {
        Name = song.name;
        Path = song.path;
    }

    /// <summary>
    /// Constructs a song with a name and file path.
    /// </summary>
    public Song(string name, string path)
    {
        Name = name;
        Path = path;
    }

    /// <summary>
    /// Implicitly converts a tuple containing song name and path into a Song object.
    /// </summary>
    /// <param name="song">A tuple containing the name and path of the song.</param>
    /// <returns>A Song object created from the provided tuple.</returns>
    public static implicit operator Song((string name, string path) song) => new(song);

    /// <summary>
    /// Implicitly converts a song path into a Song object.
    /// </summary>
    /// <param name="path">A path of the song.</param>
    /// <returns>A Song object created from the provided path.</returns>
    public static implicit operator Song(string path) => new(path);
}