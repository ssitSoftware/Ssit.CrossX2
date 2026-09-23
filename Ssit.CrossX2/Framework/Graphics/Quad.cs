namespace Ssit.CrossX2.Framework.Graphics;

public readonly struct Quad(RectangleF target, Rectangle source)
{
    public readonly RectangleF Target = target;
    public readonly Rectangle Source = source;
}