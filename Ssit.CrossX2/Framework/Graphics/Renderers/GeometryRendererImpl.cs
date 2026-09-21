using System.Numerics;

namespace Ssit.CrossX2.Framework.Graphics.Renderers;

internal class GeometryRendererImpl(IRenderQueue queue): IGeometryRenderer
{
    public void DrawLine(Vector2 v1, Vector2 v2, RgbaColor color, float depth  = 0)
    {
        queue.PushLine(
            new VertexPct(new Vector3(v1, depth), color, Vector2.Zero),
            new VertexPct(new Vector3(v2, depth), color, Vector2.Zero));
    }

    public void DrawPolyline(IReadOnlyList<Vector2> points, RgbaColor color, float depth = 0)
    {
        for (var idx = 0; idx < points.Count - 1; ++idx)
        {
            queue.PushLine(
                new VertexPct(new Vector3(points[idx], depth), color, Vector2.Zero),
                new VertexPct(new Vector3(points[idx + 1], depth), color, Vector2.Zero));
        }
    }

    public void DrawRectangle(RectangleF rect, RgbaColor color, float depth  = 0)
    {
        queue.PushLine(
            new VertexPct(new Vector3(rect.TopLeft, depth), color, Vector2.Zero),
            new VertexPct(new Vector3(rect.TopRight, depth), color, Vector2.Zero));
        
        queue.PushLine(
            new VertexPct(new Vector3(rect.TopRight, depth), color, Vector2.Zero),
            new VertexPct(new Vector3(rect.BottomRight, depth), color, Vector2.Zero));
        
        queue.PushLine(
            new VertexPct(new Vector3(rect.BottomRight, depth), color, Vector2.Zero),
            new VertexPct(new Vector3(rect.BottomLeft, depth), color, Vector2.Zero));
        
        queue.PushLine(
            new VertexPct(new Vector3(rect.BottomLeft, depth), color, Vector2.Zero),
            new VertexPct(new Vector3(rect.TopLeft, depth), color, Vector2.Zero));
    }

    public void FillRectangle(RectangleF rect, RgbaColor color, float depth  = 0)
    {
        queue.PushTriangle(
            new VertexPct(new Vector3(rect.TopLeft, depth), color, Vector2.Zero),
            new VertexPct(new Vector3(rect.BottomLeft, depth), color, Vector2.Zero),
            new VertexPct(new Vector3(rect.BottomRight, depth), color, Vector2.Zero),  
            null);
        
        queue.PushTriangle(
            new VertexPct(new Vector3(rect.TopLeft, depth), color, Vector2.Zero),
            new VertexPct(new Vector3(rect.BottomRight, depth), color, Vector2.Zero),
            new VertexPct(new Vector3(rect.TopRight, depth), color, Vector2.Zero));
    }
}