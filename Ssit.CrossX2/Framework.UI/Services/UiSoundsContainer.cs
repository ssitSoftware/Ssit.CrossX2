using Ssit.CrossX2.Framework.Audio;
using Ssit.CrossX2.Framework.Content;

namespace Ssit.CrossX2.Framework.UI.Services;

public class UiSoundsContainer(IContentManager contentManager) : IUiSounds
{
    private readonly Dictionary<string, ResourceHandle<ISoundEffect>> _sounds = new();

    ISoundEffect IUiSounds.this[string id]
    {
        get
        {
            if (_sounds.TryGetValue(id, out var sound))
            {
                return sound.Resource;
            }

            return null;
        }
    }

    IUiSounds IUiSounds.AddSound(string id, string path)
    {
        try
        {
            var sound = contentManager.Get<ISoundEffect>(path);
            _sounds.Add(id, sound);
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
            sound.Dispose();
        }

        _sounds.Clear();
    }
}