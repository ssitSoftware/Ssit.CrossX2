namespace Ssit.CrossX2.Framework.Graphics.Renderers;

public interface IQuadsRenderer
{
    void Draw(ITexture texture, RectangleF target, Rectangle? source = null, RgbaColor? color = null);
    void Draw(ITexture texture, IReadOnlyList<Quad> quads, RgbaColor color);
}