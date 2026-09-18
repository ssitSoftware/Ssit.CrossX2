using Ssit.CrossX2.Core;
using Ssit.CrossX2.IO;
using Ssit.CrossX2.IoC;
using Ssit.CrossX2.Services;

namespace Ssit.CrossX2.Audio.Internal;

// This class was partially created with Claude Code assistance
public class LoopsMusicPlayer : IMusicPlayer, IUpdatable, IDisposable
{
    private const int BufferLength = 88200 / 10;

    private readonly IFilesProvider _filesProvider;
    private readonly IIoCContainer _iocContainer;
    private readonly IActionScheduler _scheduler;

    private readonly Dictionary<string, MusicPlaylist> _playlists = new();
    private readonly List<ISingleMusicPlayer> _players = new();

    private MusicPlaylist _currentPlaylist;
    private MultiSongDataProvider _currentMusicProvider;

    private bool _isActive = true;

    private string _currentPlaylistName = "";
    
    public LoopsMusicPlayer(IFilesProvider filesProvider, IIoCContainer iocContainer, IActionScheduler scheduler, IEventSource eventSource)
    {
        _filesProvider = filesProvider;
        _iocContainer = iocContainer;
        _scheduler = scheduler;
        
        eventSource.Paused += EventSourceOnPaused;
        eventSource.Resumed += EventSourceOnResumed;
    }

    private void EventSourceOnResumed()
    {
        if (_isActive)
            return;
        
        _isActive = true;
        
        if (!string.IsNullOrWhiteSpace(_currentPlaylistName))
        {
            var name = _currentPlaylistName;
            _currentPlaylistName = "";
            ChangePlaylist(name);
        }
    }

    private void EventSourceOnPaused()
    {
        foreach (var player in _players)
        {
            player.FadeOut(10);
        }
        
        if (_currentPlaylist != null && _currentMusicProvider != null)
        {
            _currentPlaylist.CurrentSong = _currentMusicProvider.CurrentSongIndex;
            _currentPlaylist.CurrentPosition = _currentMusicProvider.CurrentSongBlock;
        }
        
        _isActive = false;
        _currentPlaylist = null;
        _currentMusicProvider = null;
    }
    
    public IMusicPlayer RegisterPlaylist(string name, params string[] songs)
    {
        var playlist = new MusicPlaylist(songs.Select(o=> new Song(o)).ToArray());
        _playlists.Add(name, playlist);
        return this;
    }
    
    public IMusicPlayer RegisterPlaylist(string name, params Song[] songs)
    {
        var playlist = new MusicPlaylist(songs);
        _playlists.Add(name, playlist);
        return this;
    }
    
    public void ResetPlaylistPosition(string name)
    {
        if (!_playlists.TryGetValue(name, out var playlist)) return;
        playlist.CurrentPosition = 0;
        playlist.CurrentSong = 0;
    }

    public void ChangePlaylist(string name, int fadeTimeMs = 250, bool resetProgress = false)
    {
        if (!_isActive)
        {
            _currentPlaylistName = name;
            return;
        }

        if (!_playlists.TryGetValue(name ?? "?????????", out var playlist))
        {
            foreach (var player in _players)
            {
                player.FadeOut(10);
            }
        
            if (_currentPlaylist != null && _currentMusicProvider != null)
            {
                _currentPlaylist.CurrentSong = _currentMusicProvider.CurrentSongIndex;
                _currentPlaylist.CurrentPosition = _currentMusicProvider.CurrentSongBlock;
            }
            
            _currentPlaylist = null;
            _currentMusicProvider = null;
            _currentPlaylistName = "";
            return;
        }

        if (ReferenceEquals(_currentPlaylist, playlist)) return;
        
        if (_currentPlaylist != null && _currentMusicProvider != null)
        {
            _currentPlaylist.CurrentSong = _currentMusicProvider.CurrentSongIndex;
            _currentPlaylist.CurrentPosition = _currentMusicProvider.CurrentSongBlock;
        }
        
        _currentPlaylist = playlist;
        _currentPlaylistName = name;
        
        var songs = playlist.List;

        Task.Run(() =>
        {
            var songIndex = _currentPlaylist.CurrentSong;
            var songBlock = _currentPlaylist.CurrentPosition;

            if (resetProgress)
            {
                songIndex = 0;
                songBlock = 0;

                _currentPlaylist.CurrentSong = 0;
                _currentPlaylist.CurrentPosition = 0;
            }
            
            var musicProvider = new MultiSongDataProvider(_filesProvider, songs, songIndex, songBlock);

            _scheduler.Schedule(() =>
            {
                foreach (var player in _players)
                {
                    player.FadeOut(fadeTimeMs);
                }

                var newPlayer = _iocContainer.IoCConstruct<ISingleMusicPlayer>();
                newPlayer.Name = name;
                
                _players.Add(newPlayer);
                newPlayer.Start(musicProvider, BufferLength, fadeTimeMs);
                _currentMusicProvider = musicProvider;
            });
        });
    }

    void IUpdatable.Update(float dt)
    {
        for (var idx = 0; idx < _players.Count;)
        {
            _players[idx].Update(dt, out var finished);

            if (finished)
            {
                _players[idx].Dispose();
                _players.RemoveAt(idx);
                continue;
            }

            ++idx;
        }
    }

    public bool NextTrack(int fadeTimeMs = 0) => false;
    public bool PreviousTrack(int fadeTimeMs = 0) => false;

    public void Dispose()
    {
        var players = _players.ToArray();
        _players.Clear();

        foreach (var player in players)
        {
            player.Dispose();
        }
    }
}
