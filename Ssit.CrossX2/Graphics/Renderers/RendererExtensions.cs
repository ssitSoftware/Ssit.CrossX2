namespace Ssit.CrossX2.Graphics.Renderers;

public static class RendererExtensions
{
    public static void DrawFrame(this IGeometryRenderer renderer, RectangleF rect, RgbaColor color, float thickness)
    {
        var topRect = new RectangleF(rect.X - thickness, rect.Y - thickness, rect.Width + thickness * 2, thickness);
        var bottomRect = new RectangleF(rect.X - thickness, rect.Bottom, rect.Width + thickness * 2, thickness);
        var leftRect = new RectangleF(rect.X - thickness, rect.Y, thickness, rect.Height);
        var rightRect = new RectangleF(rect.Right, rect.Y, thickness, rect.Height);
        
        renderer.FillRectangle(topRect, color);
        renderer.FillRectangle(bottomRect, color);
        renderer.FillRectangle(leftRect, color);
        renderer.FillRectangle(rightRect, color);
    }
}