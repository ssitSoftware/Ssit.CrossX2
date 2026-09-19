using System.Numerics;

namespace Ssit.CrossX2.Graphics.Renderers;

public interface IGeometryRenderer
{
    void DrawLine(Vector2 v1, Vector2 v2, RgbaColor color);
    void DrawPolyline(IReadOnlyList<Vector2> points, RgbaColor color);
    void DrawRectangle(RectangleF rect, RgbaColor color);
    void FillRectangle(RectangleF rect, RgbaColor color);
}