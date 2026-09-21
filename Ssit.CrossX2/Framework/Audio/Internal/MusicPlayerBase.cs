namespace Ssit.CrossX2.Framework.Audio.Internal;

public abstract class MusicPlayerBase : IMusicPlayer, IDisposable
{
    protected const int BufferLength = 88200 / 10;
    
    private readonly Dictionary<string, MusicPlaylist> _playlists = new();
    private MusicPlaylist _currentPlaylist;

    protected bool IsCurrentPlaylistSingleSong => _currentPlaylist?.List.Count == 1;

    public IMusicPlayer RegisterPlaylist(string name, params string[] songs)
    {
        var playlist = new MusicPlaylist(songs.Select(o=> new Song(o)).ToArray());
        _playlists.Add(name, playlist);
        return this;
    }

    public void ResetPlaylistPosition(string name)
    {
        if (!_playlists.TryGetValue(name, out var playlist)) return;
        playlist.CurrentPosition = 0;
        playlist.CurrentSong = 0;
    }

    public IMusicPlayer RegisterPlaylist(string name, params Song[] songs)
    {
        var playlist = new MusicPlaylist(songs);
        _playlists.Add(name, playlist);
        return this;
    }
    
    protected abstract int GetSongPosition();
    protected abstract void SwitchSong(Song song, int fadeTime, int startPosition = 0);

    public void ChangePlaylist(string name, int fadeTimeMs = 250, bool resetProgress = false)
    {
        var newPlaylist = _playlists[name];
        
        if (_currentPlaylist is not null)
        {
            if (ReferenceEquals(newPlaylist, _currentPlaylist))
                return;
            
            var currentPosition = GetSongPosition();
            _currentPlaylist.CurrentPosition = currentPosition;
        }

        _currentPlaylist = newPlaylist;

        if (resetProgress)
        {
            _currentPlaylist.CurrentSong = 0;
            _currentPlaylist.CurrentPosition = 0;
        }
        
        SwitchSong(_currentPlaylist.List[_currentPlaylist.CurrentSong], fadeTimeMs, _currentPlaylist.CurrentPosition);
    }

    public bool NextTrack(int fadeTimeMs = 0)
    {
        if (_currentPlaylist is null) return false;

        var song = GetNextSong();
        SwitchSong(song, fadeTimeMs);
        return true;
    }

    public bool PreviousTrack(int fadeTimeMs = 0)
    {
        if (_currentPlaylist is null) return false;
        
        var nextSong = _currentPlaylist.CurrentSong - 1;
        if (nextSong < 0)
        {
            nextSong += _currentPlaylist.List.Count;
        }

        _currentPlaylist.CurrentPosition = 0;
        _currentPlaylist.CurrentSong = nextSong;
        
        SwitchSong(_currentPlaylist.List[nextSong], fadeTimeMs);
        return true;
    }

    public void Restart()
    {
        // TODO: implement
    }

    public Song GetNextSong()
    {
        var nextSong = _currentPlaylist.CurrentSong + 1;
        if (_currentPlaylist.List.Count <= nextSong)
        {
            nextSong %= _currentPlaylist.List.Count;
        }
        
        _currentPlaylist.CurrentPosition = 0;
        _currentPlaylist.CurrentSong = nextSong;

        return _currentPlaylist.List[nextSong];
    }

    public void Dispose()
    {
        OnDispose();
    }

    protected virtual void OnDispose()
    {
        
    }
}