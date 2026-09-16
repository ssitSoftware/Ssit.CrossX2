using System.Drawing;
using System.Numerics;

namespace CrossX2.Graphics;

public interface IRenderStateProvider
{
    float Scale { get; }
    Vector2 Offset { get; }
    BlendMode BlendMode { get; }
    TextureFilter TextureFilter { get; }
    RectangleF? ClipRect { get; }
}