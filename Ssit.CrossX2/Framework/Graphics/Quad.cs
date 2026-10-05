namespace Ssit.CrossX2.Framework.Graphics;

public readonly struct Quad(RectangleF target, RectangleF source)
{
    public readonly RectangleF Target = target;
    public readonly RectangleF Source = source;
}