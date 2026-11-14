using SDL;
using Ssit.CrossX2.Framework.Audio;
using static SDL.SDL3;
using static SDL.SDL3_mixer;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Audio;

internal unsafe class SdlSoundEffectInstanceImpl : ISoundEffectInstance
{
    public event Action<ISoundEffectInstance> Finished;

    public ISoundListener Listener
    {
        get;
        set
        {
            field = value;
            UpdateInternalParameters();
        }
    }
    
    public ISoundEmitter Emitter
    {
        get;
        set
        {
            field = value;
            UpdateInternalParameters();
        }
    }

    public SoundParameters Parameters 
    { 
        get;
        set
        {
            field = value;
            UpdateInternalParameters();
        }
    }
    
    private SoundParameters InternalParameters
    {
        get;
        set
        {
            field = value;
            ApplyTrackParameters();
        }
    }
    
    // Time constant (s) for smoothing the emitter's radial velocity (filters camera/frame jitter).
    private const float DopplerSmoothingTime = 0.1f;

    // Pitch multiplier limits for the Doppler effect.
    private const float MinDopplerFactor = 0.5f;
    private const float MaxDopplerFactor = 2f;

    private float? _dopplerLastDistance;
    private float _dopplerRadialVelocity;
    private float _dopplerFactor = 1;

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
            MIX_SetTrackGain(_playbackTrack.Pointer, InternalParameters.Volume * _sm.SoundVolume);
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
        
        _sm.SoundVolumeUpdated -= SmOnSoundVolumeUpdated;
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
            
            ApplyTrackParameters();

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

    private void UpdateInternalParameters()
    {
        var parameters = Parameters.Apply(Emitter, Listener);
        InternalParameters = parameters with { Pitch = parameters.Pitch * _dopplerFactor };
    }

    public void ApplyDoppler(float dt, float speedOfSound = ISoundEffectInstance.DefaultSpeedOfSound)
    {
        if (Emitter is null || Listener is null || speedOfSound <= 0)
        {
            ResetDoppler();
            return;
        }

        var distance = (Emitter.Position - Listener.Position).Length();

        if (dt > 0 && _dopplerLastDistance is { } lastDistance)
        {
            // Positive = receding, negative = approaching.
            var radialVelocity = (distance - lastDistance) / dt;
            var blend = 1 - MathF.Exp(-dt / DopplerSmoothingTime);
            _dopplerRadialVelocity += (radialVelocity - _dopplerRadialVelocity) * blend;
        }

        _dopplerLastDistance = distance;

        // Classic Doppler for a source moving relative to the listener: f' = f * c / (c + v_r).
        // Clamp the velocity so the denominator stays positive (e.g. on camera snaps).
        var velocity = Math.Clamp(_dopplerRadialVelocity, -speedOfSound * 0.9f, speedOfSound * 0.9f);
        _dopplerFactor = Math.Clamp(speedOfSound / (speedOfSound + velocity), MinDopplerFactor, MaxDopplerFactor);

        UpdateInternalParameters();
    }

    private void ResetDoppler()
    {
        _dopplerLastDistance = null;
        _dopplerRadialVelocity = 0;

        if (_dopplerFactor != 1)
        {
            _dopplerFactor = 1;
            UpdateInternalParameters();
        }
    }

    /// <summary>
    /// Pushes current volume, pitch and pan to the playback track (if any), so parameter changes
    /// take effect on a sound that is already playing (e.g. looped sounds updated every frame).
    /// </summary>
    private void ApplyTrackParameters()
    {
        if (_playbackTrack.Pointer == null) return;

        MIX_SetTrackGain(_playbackTrack.Pointer, InternalParameters.Volume * _sm.SoundVolume);
        MIX_SetTrackFrequencyRatio(_playbackTrack.Pointer, InternalParameters.Pitch);

        var pan = InternalParameters.Pan;
        var gains = new MIX_StereoGains
        {
            left = 1f - MathF.Max(0f, pan),
            right = 1f + MathF.Min(0f, pan)
        };
        MIX_SetTrackStereo(_playbackTrack.Pointer, &gains);
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

        // Start fresh next time (no velocity spike from the gap).
        ResetDoppler();
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