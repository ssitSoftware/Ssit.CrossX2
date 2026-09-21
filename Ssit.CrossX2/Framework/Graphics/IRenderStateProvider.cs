using System.Numerics;

namespace Ssit.CrossX2.Framework.Graphics;

public interface IRenderStateProvider
{
    float Scale { get; }
    Vector2 Offset { get; }
    BlendMode BlendMode { get; }
    TextureFilter TextureFilter { get; }
    RectangleF? ClipRect { get; }
    IRenderTarget RenderTarget { get; }
}