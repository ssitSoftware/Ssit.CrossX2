using System.Numerics;

namespace Ssit.CrossX2.Graphics.Renderers;

internal class GeometryRendererImpl(IRenderQueue queue): IGeometryRenderer
{
    public void DrawLine(Vector2 v1, Vector2 v2, RgbaColor color)
    {
        queue.PushLine(
            new VertexPct2D(v1, color, Vector2.Zero),
            new VertexPct2D(v2, color, Vector2.Zero));
    }

    public void DrawPolyline(IReadOnlyList<Vector2> points, RgbaColor color)
    {
        for (var idx = 0; idx < points.Count - 1; ++idx)
        {
            queue.PushLine(
                new VertexPct2D(points[idx], color, Vector2.Zero),
                new VertexPct2D(points[idx + 1], color, Vector2.Zero));
        }
    }

    public void DrawRectangle(RectangleF rect, RgbaColor color)
    {
        queue.PushLine(
            new VertexPct2D(rect.TopLeft, color, Vector2.Zero),
            new VertexPct2D(rect.TopRight, color, Vector2.Zero));
        
        queue.PushLine(
            new VertexPct2D(rect.TopRight, color, Vector2.Zero),
            new VertexPct2D(rect.BottomRight, color, Vector2.Zero));
        
        queue.PushLine(
            new VertexPct2D(rect.BottomRight, color, Vector2.Zero),
            new VertexPct2D(rect.BottomLeft, color, Vector2.Zero));
        
        queue.PushLine(
            new VertexPct2D(rect.BottomLeft, color, Vector2.Zero),
            new VertexPct2D(rect.TopLeft, color, Vector2.Zero));
    }

    public void FillRectangle(RectangleF rect, RgbaColor color)
    {
        queue.PushTriangle(
            new VertexPct2D(rect.TopLeft, color, Vector2.Zero),
            new VertexPct2D(rect.BottomLeft, color, Vector2.Zero),
            new VertexPct2D(rect.BottomRight, color, Vector2.Zero),  
            null);
        
        queue.PushTriangle(
            new VertexPct2D(rect.TopLeft, color, Vector2.Zero),
            new VertexPct2D(rect.BottomRight, color, Vector2.Zero),
            new VertexPct2D(rect.TopRight, color, Vector2.Zero),
            null);
    }
}