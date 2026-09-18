using System.Numerics;

namespace Ssit.CrossX2.Graphics.Lighting;

public record struct PointLight2D(Vector2 Position, float Radius, RgbaColor Color, float Intensity);