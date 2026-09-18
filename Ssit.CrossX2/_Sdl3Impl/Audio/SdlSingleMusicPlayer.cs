using SDL;
using Ssit.CrossX2.Audio.Internal;
using static SDL.SDL3;
using static SDL.SDL3_mixer;

namespace Ssit.CrossX2._Sdl3Impl.Audio;

public unsafe class SdlSingleMusicPlayer : ISingleMusicPlayer
{
    private readonly SdlSoundManagerImpl _soundManager;

    private SdlHandle<MIX_Track> _track = SdlHandle<MIX_Track>.Empty;
    private SDL_AudioStream* _audioStream;
    private IMusicDataProvider _provider;
    private int _bufferLength;
    private short[] _shortBuffer;
    private bool _sourceExhausted;
    private bool _fadingOut;

    public int Position => _provider?.Position ?? 0;
    public string Name { get; set; } = "???";

    public SdlSingleMusicPlayer(SdlSoundManagerImpl soundManager)
    {
        _soundManager = soundManager;
        _soundManager.MusicVolumeUpdated += SoundManagerOnMusicVolumeUpdated;
    }

    private void SoundManagerOnMusicVolumeUpdated()
    {
        if (_track != null && _track.Pointer != null)
        {
            var mv = _soundManager.MusicVolume;
            MIX_SetTrackGain(_track.Pointer, mv);
        }
    }

    public void Start(IMusicDataProvider provider, int bufferLength, int fadeInMilliseconds)
    {
        _provider = provider;
        _bufferLength = bufferLength;
        _shortBuffer = new short[bufferLength];

        SDL_AudioSpec srcSpec = new SDL_AudioSpec
        {
            format = SDL_AudioFormat.SDL_AUDIO_S16LE,
            channels = 2,
            freq = provider.Frequency
        };

        _audioStream = SDL_CreateAudioStream(&srcSpec, null);

        _track = new SdlHandle<MIX_Track>(MIX_CreateTrack(_soundManager.MixerHandle.Pointer));
        MIX_SetTrackGain(_track.Pointer, _soundManager.MusicVolume);
        MIX_SetTrackAudioStream(_track.Pointer, _audioStream);

        AppendBuffer();
        AppendBuffer();

        var properties = SDL_CreateProperties();
        if (fadeInMilliseconds > 0)
        {
            SDL_SetNumberProperty(properties, MIX_PROP_PLAY_FADE_IN_MILLISECONDS_NUMBER, fadeInMilliseconds);
        }

        MIX_PlayTrack(_track.Pointer, properties);
        SDL_DestroyProperties(properties);
    }

    private void AppendBuffer()
    {
        if (_sourceExhausted) return;

        var samplesRead = _provider.Read(_shortBuffer);
        if (samplesRead == 0)
        {
            _sourceExhausted = true;
            return;
        }

        fixed (short* ptr = _shortBuffer)
        {
            SDL_PutAudioStreamData(_audioStream, (IntPtr)ptr, _shortBuffer.Length * sizeof(short));
        }
    }

    public void Update(float deltaTime, out bool finished)
    {
        finished = false;

        if (_track.Pointer == null || _audioStream == null)
        {
            finished = true;
            return;
        }

        if (_sourceExhausted || _fadingOut)
        {
            finished = !MIX_TrackPlaying(_track.Pointer);
            return;
        }

        var queuedBytes = SDL_GetAudioStreamQueued(_audioStream);
        var bytesPerBuffer = _bufferLength * sizeof(short);

        if (queuedBytes <= bytesPerBuffer)
        {
            AppendBuffer();
        }
    }

    public void FadeOut(int fadeOutMilliseconds)
    {
        if (_track.Pointer != null)
        {
            _fadingOut = true;
            MIX_StopTrack(_track.Pointer, fadeOutMilliseconds);
        }
    }

    public void Dispose()
    {
        if (_track.Pointer != null)
        {
            MIX_StopTrack(_track.Pointer, 0);
            MIX_DestroyTrack(_track.Pointer);
            _track = SdlHandle<MIX_Track>.Empty;
        }

        if (_audioStream != null)
        {
            SDL_DestroyAudioStream(_audioStream);
            _audioStream = null;
        }

        _provider?.Dispose();
        _provider = null;

        _soundManager.MusicVolumeUpdated -= SoundManagerOnMusicVolumeUpdated;
    }
}
