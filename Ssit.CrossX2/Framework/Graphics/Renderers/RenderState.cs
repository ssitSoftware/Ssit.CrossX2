using System.Numerics;

namespace Ssit.CrossX2.Framework.Graphics.Renderers;

internal struct RenderState()
{
    public Matrix4x4 Transform = Matrix4x4.Identity;
    public BlendMode BlendMode = BlendMode.AlphaBlend;
    public TextureFilter TextureFilter = TextureFilter.Point;
    public RectangleF? ClipRect = null; 
}