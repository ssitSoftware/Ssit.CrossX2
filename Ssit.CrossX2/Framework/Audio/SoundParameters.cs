namespace Ssit.CrossX2.Framework.Audio;

/// <summary>
/// SoundParameters struct holds various properties and parameters related to sound effects.
/// It is used in conjunction with sound emitting structures and interfaces to provide detailed control over sound playback characteristics.
/// </summary>
public struct SoundParameters
{
    /// <summary>
    /// Represents the parameters related to sound configuration.
    /// </summary>
    public SoundParameters()
    {
    }

    /// <summary>
    /// Gets or sets the volume level for the sound.
    /// The volume is a float value where 0 represents silence and 1 represents the maximum volume.
    /// Default value is 1.
    /// </summary>
    public float Volume { get; set; } = 1;

    /// <summary>
    /// Gets or sets the pitch level for the sound.
    /// The pitch is a float value where 1 represents the normal pitch,
    /// values greater than 1 increase the pitch and values less than 1 decrease it.
    /// The default value is 1.
    /// </summary>
    public float Pitch { get; set; } = 1;
}