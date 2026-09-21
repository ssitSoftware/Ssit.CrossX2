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
    /// Plays the sound effect once with the specified volume, pitch, and optional spatial emitter.
    /// </summary>
    void PlayOnce();
}