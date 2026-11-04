using System.Numerics;

namespace Ssit.CrossX2.Framework.Audio;

/// <summary>
/// Represents a sound listener within the audio framework.
/// This struct is used to manage and interact with sound sources in 3D space.
/// </summary>
public interface ISoundListener
{
    /// <summary>
    /// Gets or sets the position of the sound listener in 3D space.
    /// </summary>
    Vector2 Position { get; }
    float MaxDistance { get; }
    float PanDistance { get; }
}