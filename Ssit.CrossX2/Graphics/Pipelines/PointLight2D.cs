using System.Numerics;

namespace CrossX2.Graphics.Pipelines;

public readonly struct PointLight2D(Vector2 position, float radius, RgbaColor color, float intensity)
{
    public Vector2 Position { get; } = position;
    public float Radius { get; } = radius;
    public RgbaColor Color { get; } = color;
    public float Intensity { get; } = intensity;
}