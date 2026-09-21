using SDL;
using Ssit.CrossX2.Framework.Audio;
using Ssit.CrossX2.Framework.Core;
using static SDL.SDL3;
using static SDL.SDL3_mixer;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Audio;

internal unsafe class SdlSoundManagerImpl: ISoundManager, IUpdatable
{
    public event Action SoundVolumeUpdated;
    public event Action MusicVolumeUpdated;
    public event Action Disposing;
    public ISoundListener SoundListener { get; set; }

    public readonly SdlHandle<MIX_Mixer> MixerHandle;
    private readonly List<SdlSoundEffectInstanceImpl> _attachedInstances = new();
    
    private float _soundVolume = 1;
    private float _musicVolume = 1;

    public float SoundVolume
    {
        get => _soundVolume;

        set
        {
            _soundVolume = value;
            SoundVolumeUpdated?.Invoke();
        }
    }

    public float MusicVolume
    {
        get => _musicVolume;
        set
        {
            _musicVolume = value;
            MusicVolumeUpdated?.Invoke();
        }
    }

    public SdlSoundManagerImpl()
    {
        MIX_Init();

        SDL_AudioSpec spec = new SDL_AudioSpec
        {
            format = SDL_AudioFormat.SDL_AUDIO_S16LE,
            channels = 2,
            freq = 44100
        };
        
        MixerHandle = new SdlHandle<MIX_Mixer>(MIX_CreateMixerDevice(SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK, &spec));
    }
    
    public void Dispose()
    {
        Disposing?.Invoke();

        if (MixerHandle.Pointer == null) return;
        
        MIX_DestroyMixer(MixerHandle.Pointer);
        MixerHandle.OnDisposed();
    }

    void IUpdatable.Update(float dt)
    {
        for (var idx =0; idx < _attachedInstances.Count;)
        {
            var inst = _attachedInstances[idx];
            if (inst.CheckPlaying())
            {
                ++idx;
            }
        }
    }

    public void AttachInstance(SdlSoundEffectInstanceImpl sdlSoundEffectInstanceImpl)
    {
        _attachedInstances.Add(sdlSoundEffectInstanceImpl);   
    }

    public void DetachInstance(SdlSoundEffectInstanceImpl sdlSoundEffectInstanceImpl)
    {
        _attachedInstances.Remove(sdlSoundEffectInstanceImpl);
    }

    public void ForceFreeTracks()
    {
        foreach (var inst in _attachedInstances)
        {
            if (!inst.ForceFreeTrack()) continue;
            
            DetachInstance(inst);
            break;
        }
    }
}