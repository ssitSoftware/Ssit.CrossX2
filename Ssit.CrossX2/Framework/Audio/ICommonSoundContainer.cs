namespace Ssit.CrossX2.Framework.Audio;

public interface ICommonSoundContainer
{
    void Play(string name, float volume = 1, ISoundEmitter emitter = null);
    ICommonSoundContainer RegisterSound(string name, string path, float volume = 1);
}