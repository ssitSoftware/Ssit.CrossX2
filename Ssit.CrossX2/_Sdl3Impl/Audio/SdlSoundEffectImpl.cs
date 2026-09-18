using SDL;
using Ssit.CrossX2.Audio;
using Ssit.CrossX2.Audio.Internal;
using Ssit.CrossX2.IoC;
using static SDL.SDL3_mixer;

namespace Ssit.CrossX2._Sdl3Impl.Audio;

public unsafe class SdlSoundEffectImpl: ISoundEffect
{
    private readonly IIoCContainer _iocContainer;
    private readonly SdlSoundManagerImpl _soundManager;
    private readonly SdlHandle<MIX_Audio> _audioHandle;
    
    private readonly List<ISoundEffectInstance> _instances = new();
    
    public SdlSoundEffectImpl(IIoCContainer iocContainer, SdlSoundManagerImpl soundManager, Stream stream)
    {
        _iocContainer = iocContainer;
        _soundManager = soundManager;
        
        _soundManager.Disposing += Dispose;

        var wavFile = WavLoader.LoadMonoWav(stream);

        fixed (short* shrt = wavFile.buffer)
        {
            IntPtr bytesPtr = (IntPtr)shrt;
            var bufferLen = wavFile.buffer.Length * sizeof(short);

            SDL_AudioSpec spec = new SDL_AudioSpec
            {
                channels = 1,
                format = SDL_AudioFormat.SDL_AUDIO_S16LE,
                freq = wavFile.sampleRate
            };
            
            var chunk = MIX_LoadRawAudio(soundManager.MixerHandle.Pointer, bytesPtr, (UIntPtr)bufferLen, &spec);
            _audioHandle = new SdlHandle<MIX_Audio>(chunk);
        }
    }

    public void Dispose()
    {
        _soundManager.Disposing -= Dispose;
        
        foreach (var instance in _instances)
        {
            instance.Dispose();
        }
        _instances.Clear();
        
        if (_audioHandle.Pointer != null)
        {
            MIX_DestroyAudio(_audioHandle.Pointer);
            _audioHandle.OnDisposed();
        }
    }

    public ISoundEffectInstance CreateInstance()
    {
        return _iocContainer.IoCConstruct<SdlSoundEffectInstanceImpl>(_audioHandle);
    }

    public void PlayOnce()
    {
        var instance = CreateInstance();
        
        _instances.Add(instance);
        
        instance.Finished += InstanceOnFinished;
        instance.Parameters = new SoundParameters();
        instance.Play();
    }

    private void InstanceOnFinished(ISoundEffectInstance instance)
    {
        _instances.Remove(instance);
        
        instance.Finished -= InstanceOnFinished;
        instance.Dispose();
    }
}