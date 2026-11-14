namespace Ssit.CrossX2.Framework.Audio;

/// <summary>
/// Represents an instance of a sound effect, supporting operations like play, stop, pause, and resume.
/// </summary>
public interface ISoundEffectInstance: IDisposable
{
    /// <summary>
    /// Default speed of sound in logical units (tiles) per second used by <see cref="ApplyDoppler"/>.
    /// Much lower than a realistic value, so in-game speeds produce an audible shift.
    /// </summary>
    const float DefaultSpeedOfSound = 50f;

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
    
    ISoundListener Listener { get; set; }
    
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

    /// <summary>
    /// Updates the Doppler effect from how fast the distance between <see cref="Emitter"/> and
    /// <see cref="Listener"/> changes (approaching = higher pitch, receding = lower) and applies it
    /// on top of <see cref="Parameters"/> pitch. Call every update while the sound is playing.
    /// No effect without an emitter and a listener. The Doppler state resets when the sound is stopped.
    /// </summary>
    /// <param name="dt">Time since the previous call in seconds.</param>
    /// <param name="speedOfSound">Speed of sound in logical units per second; 0 or less disables the effect.</param>
    void ApplyDoppler(float dt, float speedOfSound = DefaultSpeedOfSound);
}