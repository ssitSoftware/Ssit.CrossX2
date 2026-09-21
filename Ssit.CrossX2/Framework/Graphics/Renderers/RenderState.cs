using System.Numerics;

namespace Ssit.CrossX2.Framework.Graphics.Renderers;

internal readonly struct RenderState(float scale, Vector2 offset, BlendMode blendMode, TextureFilter textureFilter, RectangleF? clipRect)
{
    public readonly float Scale = scale;
    public readonly Vector2 Offset = offset;
    public readonly BlendMode BlendMode = blendMode;
    public readonly TextureFilter TextureFilter = textureFilter;
    public readonly RectangleF? ClipRect = clipRect; 
}