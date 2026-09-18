namespace Ssit.CrossX2.Audio;

/// <summary>
/// Defines the interface for managing sound within the framework.
/// </summary>
public interface ISoundManager: IDisposable
{
    /// <summary>
    /// Occurs when the master volume level is updated.
    /// </summary>
    /// <remarks>
    /// This event is triggered whenever there is a change to the master volume.
    /// It provides the new volume level as a parameter, represented as a value between 0.0 and 1.0.
    /// </remarks>
    event Action SoundVolumeUpdated;

    event Action Disposing;
    
    /// <summary>
    /// Gets or sets the master volume level for the sound.
    /// </summary>
    /// <remarks>
    /// The master volume is a value between 0.0 and 1.0, where 0.0 means no sound and 1.0 means full volume.
    /// Changes to this property will affect the overall volume of all sounds in game/app.
    /// </remarks>
    float SoundVolume { get; set; }

    /// <summary>
    /// Gets or sets the master volume level for the sound.
    /// </summary>
    /// <remarks>
    /// The master volume is a value between 0.0 and 1.0, where 0.0 means no sound and 1.0 means full volume.
    /// Changes to this property will affect the overall volume of all sounds in game/app.
    /// </remarks>
    float MusicVolume { get; set; }
    
    /// <summary>
    /// Gets or sets the sound listener for the audio framework.
    /// </summary>
    /// <remarks>
    /// This property represents the sound listener which is responsible for interpreting sound sources
    /// in the 3D space within the audio framework. Adjusting the sound listener's position, velocity,
    /// and direction can affect how 3D sound is perceived.
    /// </remarks>
    ISoundListener SoundListener { get; set; }
}