using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.IO;
using Ssit.CrossX2.Framework.IoC;
using Ssit.CrossX2.Framework.Services;

namespace Ssit.CrossX2.Framework.Audio.Internal;

public class BufferMusicPlayer : MusicPlayerBase, IUpdatable
{
    private readonly IFilesProvider _filesProvider;
    private readonly IIoCContainer _iocContainer;
    private readonly IActionScheduler _scheduler;

    private readonly List<ISingleMusicPlayer> _musicPlayers = new();
    private bool _switching;

    public float Volume { get; set; } = 0.5f;

    public BufferMusicPlayer(IFilesProvider filesProvider, IIoCContainer iocContainer, IActionScheduler scheduler)
    {
        _filesProvider = filesProvider;
        _iocContainer = iocContainer;
        _scheduler = scheduler;
    }

    void IUpdatable.Update(float dt)
    {
        for (var idx = 0; idx < _musicPlayers.Count;)
        {
            _musicPlayers[idx].Update(dt, out var finished);

            if (finished)
            {
                _musicPlayers[idx].Dispose();
                _musicPlayers.RemoveAt(idx);
                continue;
            }

            ++idx;
        }

        if (_musicPlayers.Count == 0 && !_switching)
        {
            NextTrack();
        }
    }

    protected override void OnDispose()
    {
        var players = _musicPlayers.ToArray();
        _musicPlayers.Clear();

        foreach (var player in players)
        {
            player.Dispose();
        }
    }

    protected override int GetSongPosition()
    {
        return _musicPlayers.LastOrDefault()?.Position ?? 0;
    }

    protected override void SwitchSong(Song song, int fadeTime, int startPosition = 0)
    {
        fadeTime = Math.Max(50, fadeTime);
        _switching = true;

        Task.Run(() =>
        {
            IMusicDataProvider provider;
            if (IsCurrentPlaylistSingleSong)
            {
                provider = new MultiSongDataProvider(_filesProvider, new[] { song });
            }
            else
            {
                var songStream = _filesProvider.Open(song.Path);
                var vorbis = new VorbisDataProvider(songStream);
                if (startPosition > 0) vorbis.Skip(startPosition, BufferLength);
                provider = vorbis;
            }

            _scheduler.Schedule(() =>
            {
                _switching = false;

                foreach (var player in _musicPlayers)
                {
                    player.FadeOut(fadeTime);
                }

                var newPlayer = _iocContainer.IoCConstruct<ISingleMusicPlayer>();
                newPlayer.Name = song.Name;
                _musicPlayers.Add(newPlayer);
                newPlayer.Start(provider, BufferLength, fadeTime);
            });
        });
    }
}
