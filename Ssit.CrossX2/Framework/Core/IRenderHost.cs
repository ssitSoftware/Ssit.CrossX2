using System.Numerics;
using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.Core;

/// <summary>
/// Owns render-host scaling state and coordinate transforms for the current frame.
/// </summary>
public interface IRenderHost : IDisposable
{
    public Matrix3x2 Transform { get; }
    public Matrix3x2 TransformInv { get; }
    Size TargetSize { get; }
    Size LogicalSize => TargetSize / Scale;
    int Scale { get; }
    void Apply();

    /// <summary>
    /// Texture with the last rendered frame, before CRT simulation and post rendering (glow pass already applied).
    /// Null until the first frame is rendered. Owned by the render host - don't dispose it.
    /// </summary>
    ITexture SceneTexture { get; }
}
