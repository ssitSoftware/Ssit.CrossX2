using Ssit.CrossX2.Framework.Audio;

namespace Ssit.CrossX2.Framework.UI.Services;

public interface IUiSounds: IDisposable
{
    ISoundEffect this[string id] { get; }

    /// <summary>
    /// Registers a UI sound under the given id.
    /// </summary>
    /// <param name="id">Sound id (see <see cref="UiSounds"/>).</param>
    /// <param name="path">Content path of the sound.</param>
    /// <param name="volume">Volume applied every time this sound is played (1 = full volume).</param>
    IUiSounds AddSound(string id, string path, float volume = 1f);
}