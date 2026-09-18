using Ssit.CrossX2.Audio;

namespace Ssit.CrossX2.UI.Services;

public interface IUiSounds: IDisposable
{
    ISoundEffect this[string id] { get; }

    IUiSounds AddSound(string id, string path);
}