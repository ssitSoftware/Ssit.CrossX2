using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Services;

namespace Ssit.CrossX2.Framework.Audio;

internal class CommonSoundContainer(IContentManager contentManager, IActionScheduler scheduler) : ICommonSoundContainer, IDisposable
{
    private readonly Dictionary<string, (ResourceHandle<ISoundEffect>, float, List<ISoundEffectInstance>)> _sounds = new();

    public ICommonSoundContainer RegisterSound(string name, string path, float volume = 1)
    {
        var sound = contentManager.Get<ISoundEffect>(path);
        _sounds.Add(name, (sound, volume, new()));
        return this;
    }

    public void Play(string name, float volume = 1, ISoundEmitter emitter = null)
    {
        scheduler.ExecuteOnMainThread(() =>
        {
            if (!_sounds.TryGetValue(name, out var sound))
            {
                return;
            }

            ISoundEffectInstance instance = null;
            foreach (var inst in sound.Item3)
            {
                if (!inst.IsPlaying)
                {
                    instance = inst;
                    break;
                }
            }

            if (instance == null)
            {
                instance = sound.Item1.Resource.CreateInstance();
                sound.Item3.Add(instance);
            }

            instance.Parameters = new SoundParameters
            {
                Volume = volume * sound.Item2
            };
            instance.Play();
        });
    }

    public void Dispose()
    {
        foreach (var sound in _sounds)
        {
            foreach (var inst in sound.Value.Item3)
            {
                inst.Dispose();
            }
            
            sound.Value.Item1.Dispose();
        }
        _sounds.Clear();
    }
}