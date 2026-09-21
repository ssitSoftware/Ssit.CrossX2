namespace Ssit.CrossX2.Framework.Audio;

/// <summary>
/// Represents a music playlist that holds a collection of songs and provides functionality to manage them.
/// </summary>
internal class MusicPlaylist(Song[] songs)
{
    /// <summary>
    /// Represents the list of songs in the music playlist.
    /// </summary>
    private readonly List<Song> _playlist = [..songs];

    /// <summary>
    /// Gets or sets the current position in milliseconds of the song being played in the playlist.
    /// </summary>
    internal int CurrentPosition { get; set; }

    /// <summary>
    /// Gets or sets the index of the currently playing song in the playlist.
    /// </summary>
    internal int CurrentSong { get; set; }

    /// <summary>
    /// Gets a read-only list of songs in the music playlist.
    /// </summary>
    internal IReadOnlyList<Song> List => _playlist;
}