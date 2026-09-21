using System.Numerics;

namespace Ssit.CrossX2.Framework.Graphics.Renderers;

public interface IGeometryRenderer
{
    void DrawLine(Vector2 v1, Vector2 v2, RgbaColor color, float depth  = 0);
    void DrawPolyline(IReadOnlyList<Vector2> points, RgbaColor color, float depth  = 0);
    void DrawRectangle(RectangleF rect, RgbaColor color, float depth  = 0);
    void FillRectangle(RectangleF rect, RgbaColor color, float depth  = 0);
}