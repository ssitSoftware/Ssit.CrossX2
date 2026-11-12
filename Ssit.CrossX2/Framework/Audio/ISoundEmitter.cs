using System.Numerics;

namespace Ssit.CrossX2.Framework.Audio;

/// <summary>
/// Represents the position of a sound source for spatial audio effects.
/// </summary>
public interface ISoundEmitter
{
    Vector2 Position { get; }
}
