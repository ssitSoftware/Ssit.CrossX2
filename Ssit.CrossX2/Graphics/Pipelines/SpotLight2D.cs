using System.Numerics;

namespace CrossX2.Graphics.Pipelines;

public readonly struct SpotLight2D(Vector2 position, Vector2 direction, float outerAngle, float innerAngle, float radius, RgbaColor color, float intensity)
{
    public Vector2 Position { get; } = position;
    public Vector2 Direction { get; } = Vector2.Normalize(direction);
    public float OuterAngle { get; } = outerAngle;
    public float InnerAngle { get; } = innerAngle;
    public float Radius { get; } = radius;
    public RgbaColor Color { get; } = color;
    public float Intensity { get; } = intensity;
}
