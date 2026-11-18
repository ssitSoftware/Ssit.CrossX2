using Ssit.CrossX2.Framework.Audio;
using Ssit.CrossX2.Framework.Content;

namespace Ssit.CrossX2.Framework.UI.Services;

public class UiSoundsContainer(IContentManager contentManager) : IUiSounds
{
    private readonly Dictionary<string, UiSound> _sounds = new();

    ISoundEffect IUiSounds.this[string id]
    {
        get
        {
            if (_sounds.TryGetValue(id, out var sound))
            {
                return sound;
            }

            return null;
        }
    }

    IUiSounds IUiSounds.AddSound(string id, string path, float volume)
    {
        try
        {
            var sound = contentManager.Get<ISoundEffect>(path);
            _sounds.Add(id, new UiSound(sound, MathF.Max(0, volume)));
        }
        catch (FileNotFoundException)
        {
            // Ignore this exception
        }

        return this;
    }

    public void Dispose()
    {
        foreach (var sound in _sounds.Values)
        {
            sound.Handle.Dispose();
        }

        _sounds.Clear();
    }

    /// <summary>
    /// Wraps a sound resource and applies the volume configured in <see cref="IUiSounds.AddSound"/>
    /// whenever the sound is played. The underlying resource is owned by the container.
    /// </summary>
    private sealed class UiSound(ResourceHandle<ISoundEffect> handle, float volume) : ISoundEffect
    {
        public ResourceHandle<ISoundEffect> Handle { get; } = handle;

        public ISoundEffectInstance CreateInstance()
        {
            var instance = Handle.Resource.CreateInstance();
            instance.Parameters = new SoundParameters(volume);
            return instance;
        }

        public void PlayOnce() => PlayOnce(new SoundParameters());

        public void PlayOnce(SoundParameters parameters)
            => Handle.Resource.PlayOnce(parameters with { Volume = parameters.Volume * volume });

        public void Dispose()
        {
            // Owned by UiSoundsContainer – disposed in UiSoundsContainer.Dispose().
        }
    }
}