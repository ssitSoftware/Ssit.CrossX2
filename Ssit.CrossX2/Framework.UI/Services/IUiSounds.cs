using Ssit.CrossX2.Framework.Audio;

namespace Ssit.CrossX2.Framework.UI.Services;

public interface IUiSounds: IDisposable
{
    ISoundEffect this[string id] { get; }

    IUiSounds AddSound(string id, string path);
}