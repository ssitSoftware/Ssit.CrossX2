using System.Numerics;

namespace Ssit.CrossX2.Framework.Graphics;

public interface IRenderStateProvider
{
    Matrix4x4 Transform { get; }
    BlendMode BlendMode { get; }
    TextureFilter TextureFilter { get; }
    RectangleF? ClipRect { get; }
    float Scale { get;}
}