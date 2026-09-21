using Ssit.CrossX2.Framework.Audio;
using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Audio;

public class ContextSoundContainer(IContentManager contentManager, ContextSoundContainer.Parameters parameters)
    : IDisposable
{
    public class Parameters
    {
        public ISoundEmitter Emitter;
    }

    private readonly Dictionary<(string, int), (ISoundEffectInstance, float)> _instances = new();
    private readonly List<ResourceHandle<ISoundEffect>> _sounds = new();

    public ContextSoundContainer RegisterSound(string name, string path, float volume = 1)
    {
        return RegisterSound(name, -1, path, volume);
    }
    
    public ContextSoundContainer RegisterSound(string name, int material, string path, float volume = 1)
    {
        var sound = contentManager.Get<ISoundEffect>(path);
        var instance = sound.Resource.CreateInstance();
        
        instance.Parameters = new SoundParameters
        {
            Volume = 1,
            Pitch = 1
        };
        instance.Emitter = parameters.Emitter;
        
        _sounds.Add(sound);
        _instances.Add((name, material), (instance, volume));
        return this;
    }

    public void PlayLoop(string name)
    {
        if (!_instances.TryGetValue((name, -1), out var instance))
        {
            return;
        }

        if (!instance.Item1.IsPlaying)
        {
            instance.Item1.Play(true);
        }
    }

    public void StopLoop(string name)
    {
        if (!_instances.TryGetValue((name, -1), out var instance))
        {
            return;
        }
        instance.Item1.Stop();
    }

    public void Play(string name, IMaterial material = null, float volume = 1, bool dontStop = false)
    {
        var materialIndex = material?.Index ?? -1;
        if (!_instances.TryGetValue((name, materialIndex), out var instance))
        {
            if (!_instances.TryGetValue((name, -1), out instance))
            {
                return;
            }
        }

        if (instance.Item1.IsPlaying)
        {
            if (dontStop)
                return;
            
            instance.Item1.Stop();
        }

        instance.Item1.Parameters = new SoundParameters
        {
            Volume = instance.Item2 * volume
        };

        instance.Item1.Play();
    }
    
    public void Dispose()
    {
        foreach (var instance in _instances)
        {
            instance.Value.Item1.Dispose();
        }
        _instances.Clear();
        
        foreach (var sound in _sounds)
        {
            sound.Dispose();
        }
        _sounds.Clear();
    }

    public void StopAll()
    {
        foreach (var instance in _instances)
        {
            instance.Value.Item1.Stop();
        }
    }
}