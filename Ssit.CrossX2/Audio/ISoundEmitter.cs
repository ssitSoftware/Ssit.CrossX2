using System.Numerics;

namespace Ssit.CrossX2.Audio;

/// <summary>
/// The SoundEmitter struct is used to represent the position and velocity of a sound source in 3D space.
/// It is primarily used in conjunction with sound effect instances to provide spatial audio effects.
/// </summary>
public interface ISoundEmitter
{
    /// <summary>
    /// Gets or sets the position of the sound emitter in 3D space.
    /// </summary>
    Vector3 Position { get; }

    /// <summary>
    /// Gets or sets the velocity of the sound emitter.
    /// The velocity determines the speed and direction at which the emitter is moving.
    /// </summary>
    Vector3 Velocity => Vector3.Zero;

    /// <summary>
    /// Gets the direction in which the sound emitter is facing in 3D space.
    /// </summary>
    Vector3 Direction => new Vector3(0, 0, 1);
    
    /// <summary>
    /// Event triggered when the sound emitter's parameters are updated.
    /// </summary>
    event Action ParametersUpdated;
}