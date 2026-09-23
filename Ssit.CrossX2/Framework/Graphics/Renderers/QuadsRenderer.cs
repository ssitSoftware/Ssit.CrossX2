
namespace Ssit.CrossX2.Framework.Graphics.Renderers;

internal class QuadsRenderer(IRenderQueue renderQueue): IQuadsRenderer
{
    public void Draw(ITexture texture, RectangleF target, Rectangle? nullableSource = null, RgbaColor? colorAttr = null)
    {
        var source = nullableSource ?? new Rectangle(0, 0, texture.Size.Width, texture.Size.Height);
        var color = colorAttr ?? RgbaColor.White;
        
        var texRect = new RectangleF(source.X / (float)texture.Size.Width, source.Y / (float)texture.Size.Height, 
            source.Width / (float)texture.Size.Width, source.Height / (float)texture.Size.Height);
        
        renderQueue.PushTriangle(new VertexPct(target.TopLeft, color, texRect.TopLeft),
            new VertexPct(target.BottomLeft, color, texRect.BottomLeft),
            new VertexPct(target.BottomRight, color, texRect.BottomRight), texture);
        
        renderQueue.PushTriangle(new VertexPct(target.TopLeft, color, texRect.TopLeft),
            new VertexPct(target.BottomRight, color, texRect.BottomRight), 
            new VertexPct(target.TopRight, color, texRect.TopRight),
            texture);
    }

    public void Draw(ITexture texture, IReadOnlyList<Quad> quads, RgbaColor colorAttr)
    {
        for (var idx = 0; idx < quads.Count; ++idx)
        {
            Draw(texture, quads[idx].Target, quads[idx].Source, colorAttr);
        }
    }
}