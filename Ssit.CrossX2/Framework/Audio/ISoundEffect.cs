namespace Ssit.CrossX2.Framework.Audio;

/// <summary>
/// Defines the interface for a sound effect. Provides methods to create
/// instances of the sound effect and play the sound effect.
/// </summary>
public interface ISoundEffect: IDisposable
{
    /// <summary>
    /// Creates a new instance of the sound effect.
    /// </summary>
    /// <returns>
    /// New sound effect instance object.
    /// </returns>
    ISoundEffectInstance CreateInstance();

    /// <summary>
    /// Plays the sound effect once with default parameters.
    /// </summary>
    void PlayOnce() => PlayOnce(new SoundParameters());

    /// <summary>
    /// Plays the sound effect once with the specified parameters (volume, pitch, pan).
    /// </summary>
    /// <param name="parameters">Playback parameters.</param>
    void PlayOnce(SoundParameters parameters);
}