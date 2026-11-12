using System.Numerics;

namespace Ssit.CrossX2.Framework.Audio;

/// <summary>
/// Registers and plays named sound effects shared across the application.
/// </summary>
public interface ICommonSoundContainer
{
    void Play(string name, float volume = 1, float pitch = 1, ISoundEmitter emitter = null);
    ICommonSoundContainer RegisterSound(string name, string path, float volume = 1);
}
