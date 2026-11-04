using System.Numerics;

namespace Ssit.CrossX2.Framework.Audio;

/// <summary>
/// The SoundEmitter struct is used to represent the position and velocity of a sound source in 3D space.
/// It is primarily used in conjunction with sound effect instances to provide spatial audio effects.
/// </summary>
public interface ISoundEmitter
{
    Vector2 Position { get; }
}