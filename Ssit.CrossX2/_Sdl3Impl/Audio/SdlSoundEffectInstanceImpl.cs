using SDL;
using Ssit.CrossX2.Audio;
using static SDL.SDL3;
using static SDL.SDL3_mixer;

namespace Ssit.CrossX2._Sdl3Impl.Audio;

public unsafe class SdlSoundEffectInstanceImpl : ISoundEffectInstance
{
    public event Action<ISoundEffectInstance> Finished;
    public ISoundEmitter Emitter { get; set; }
    public SoundParameters Parameters { get; set; }
    
    private bool _isLooped;
    private bool _isPaused;

    public bool IsPlaying
    {
        get
        {
            if (_playbackTrack.Pointer == null)
            {
                return false;
            }
            
            return MIX_TrackPlaying(_playbackTrack.Pointer);
        }
    }

    private SdlHandle<MIX_Track> _playbackTrack = SdlHandle<MIX_Track>.Empty;
    private readonly SdlSoundManagerImpl _sm;
    private readonly SdlHandle<MIX_Audio> _chunk;
    private readonly SdlTrackPool _trackPool;
    private readonly SDL_PropertiesID _properties;
    private long _startPosition;
    private readonly long _duration;

    public SdlSoundEffectInstanceImpl(SdlSoundManagerImpl sm, SdlHandle<MIX_Audio> chunk, SdlTrackPool trackPool)
    {
        _sm = sm;
        _sm.AttachInstance(this);
        _chunk = chunk;
        _trackPool = trackPool;
        
        _properties = SDL_CreateProperties();
        _duration = MIX_GetAudioDuration(_chunk.Pointer);
        
        sm.SoundVolumeUpdated += SmOnSoundVolumeUpdated;
    }

    private void SmOnSoundVolumeUpdated()
    {
        if (_playbackTrack != null && _playbackTrack.Pointer != null)
        {
            MIX_SetTrackGain(_playbackTrack.Pointer, Parameters.Volume * _sm.SoundVolume);
        }
    }

    public bool CheckPlaying()
    {
        if (_playbackTrack.Pointer != null)
        {
            var position = MIX_GetTrackPlaybackPosition(_playbackTrack.Pointer);
            if (!MIX_TrackPlaying(_playbackTrack.Pointer) ||  position >= _duration)
            {
                _trackPool.ReturnTrack(_playbackTrack);
                _playbackTrack = SdlHandle<MIX_Track>.Empty;
                Finished?.Invoke(this);
                return false;
            }
        }

        return true;
    }

    public void Dispose()
    {
        if (_playbackTrack.Pointer != null)
        {
            MIX_StopTrack(_playbackTrack.Pointer, 0);
            _trackPool.ReturnTrack(_playbackTrack);
            _playbackTrack = SdlHandle<MIX_Track>.Empty;
        }
        
        _sm.DetachInstance(this);
        SDL_DestroyProperties(_properties);
        
        _sm.SoundVolumeUpdated += SmOnSoundVolumeUpdated;
    }
    
    public void Play(bool loop = false)
    {
        _isPaused = false;
        
        if (_playbackTrack.Pointer == null)
        {
            _playbackTrack = _trackPool.GetAvailableTrack();

            if (_playbackTrack.Pointer is null)
            {
                _sm.ForceFreeTracks();
                _playbackTrack = _trackPool.GetAvailableTrack();

                if (_playbackTrack.Pointer is null)
                {
                    return;
                }
            }
            
            MIX_SetTrackGain(_playbackTrack.Pointer, Parameters.Volume * _sm.SoundVolume);
            MIX_SetTrackPlaybackPosition(_playbackTrack.Pointer, _startPosition);
            MIX_SetTrackAudio(_playbackTrack.Pointer, _chunk.Pointer);
            
            SDL_SetNumberProperty(_properties, MIX_PROP_PLAY_LOOPS_NUMBER, loop ? -1 : 0);
            
            if (MIX_PlayTrack(_playbackTrack.Pointer, _properties) == false)
            {
                Console.WriteLine("Failed to play audio");
            }
            _isLooped = loop;
            _startPosition = 0;
        }
    }

    public void Stop()
    {
        if (_playbackTrack.Pointer != null)
        {
            MIX_StopTrack(_playbackTrack.Pointer, 0);
            _trackPool.ReturnTrack(_playbackTrack);
            _playbackTrack = SdlHandle<MIX_Track>.Empty;
        }

        _startPosition = 0;
        _isPaused = false;
    }

    public void Pause()
    {
        if (_playbackTrack.Pointer != null)
        {
            MIX_PauseTrack(_playbackTrack.Pointer);
            _startPosition = MIX_GetTrackPlaybackPosition(_playbackTrack.Pointer);
            _isPaused = true;
        }
    }

    public void Resume()
    {
        if (_playbackTrack.Pointer != null)
        {
            MIX_ResumeTrack(_playbackTrack.Pointer);
            _startPosition = 0;
        }
        else
        {
            Play(_isLooped);
        }

        _isPaused = false;
    }

    public bool ForceFreeTrack()
    {
        if (_isPaused)
        {
            if (_playbackTrack.Pointer != null)
            {
                MIX_StopTrack(_playbackTrack.Pointer, 0);
                _trackPool.ReturnTrack(_playbackTrack);
                _playbackTrack = SdlHandle<MIX_Track>.Empty;
                return true;
            }
        }
        
        if (_isLooped)
            return false;
        
        MIX_StopTrack(_playbackTrack.Pointer, 0);
        _trackPool.ReturnTrack(_playbackTrack);
        _playbackTrack = SdlHandle<MIX_Track>.Empty;
        return true;
    }
}