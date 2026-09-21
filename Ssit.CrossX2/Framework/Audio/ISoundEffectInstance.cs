namespace Ssit.CrossX2.Framework.Audio;

/// <summary>
/// Represents an instance of a sound effect, supporting operations like play, stop, pause, and resume.
/// </summary>
public interface ISoundEffectInstance: IDisposable
{
    /// <summary>
    /// Event that is triggered when the playback of the sound effect instance is finished.
    /// </summary>
    event Action<ISoundEffectInstance> Finished;

    /// <summary>
    /// Gets or sets the sound emitter associated with the sound effect instance.
    /// The sound emitter provides spatial audio effects by representing the position
    /// and velocity of the sound source in 3D space.
    /// </summary>
    ISoundEmitter Emitter { get; set; }
    
    /// <summary>
    /// Gets or sets the sound parameters associated with the sound effect instance.
    /// </summary>
    /// <remarks>
    /// These parameters define various properties of the sound effect such as volume, pitch, etc.
    /// </remarks>
    SoundParameters Parameters { get; set; }

    /// <summary>
    /// Indicates whether the sound effect instance is currently playing.
    /// </summary>
    bool IsPlaying { get; }


    /// <summary>
    /// Plays the sound effect instance.
    /// </summary>
    /// <param name="loop">If set to true, the sound effect will loop until stopped.</param>
    void Play(bool loop = false);

    /// <summary>
    /// Stops the playback of the sound effect instance that is currently playing.
    /// </summary>
    void Stop();

    /// <summary>
    /// Pauses the sound effect instance.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes a paused sound effect instance.
    /// </summary>
    void Resume();
}