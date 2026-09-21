namespace Ssit.CrossX2.Framework.Audio;

/// <summary>
/// Defines methods for controlling music playback and managing playlists.
/// </summary>
public interface IMusicPlayer
{
    /// <summary>Registers a new music playlist with the specified name.</summary>
    /// <param name="name">The name of the playlist to register.</param>
    /// <param name="songs">An array of songs to include in the playlist.</param>
    /// <returns>An instance of <see cref="IMusicPlayer"/> to allow for method chaining.</returns>
    IMusicPlayer RegisterPlaylist(string name, params Song[] songs);

    /// <summary>Resets the playback position of the specified playlist to the beginning.</summary>
    /// <param name="name">The name of the playlist whose position is to be reset.</param>
    void ResetPlaylistPosition(string name);

    /// <summary>Changes the current music playlist to the specified one.</summary>
    /// <param name="name">The name of the playlist to switch to.</param>
    /// <param name="fadeTimeMs">The time it takes to fade out the current playlist and fade in the new one, in milliseconds.</param>
    /// <param name="resetProgress">Specifies whether to reset the new playlist's progress to the start.</param>
    void ChangePlaylist(string name, int fadeTimeMs = 250, bool resetProgress = false);

    /// <summary>Advances to the next track in the current playlist.</summary>
    /// <param name="fadeTimeMs">The time in milliseconds to fade out the current track and fade in the next track.</param>
    /// <returns>Returns true if the operation was successful; otherwise, false.</returns>
    bool NextTrack(int fadeTimeMs = 0);

    /// <summary>Plays the previous track in the current playlist.</summary>
    /// <param name="fadeTimeMs">The time in milliseconds over which to fade the track in and out.</param>
    /// <returns>True if the previous track was successfully selected; otherwise, false.</returns>
    bool PreviousTrack(int fadeTimeMs = 0);
}