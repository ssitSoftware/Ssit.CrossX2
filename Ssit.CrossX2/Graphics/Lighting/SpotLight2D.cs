using System.Numerics;

namespace Ssit.CrossX2.Graphics.Lighting;

public record struct SpotLight2D(Vector2 Position, Vector2 Direction, float OuterAngle, float InnerAngle, float Radius, RgbaColor Color, float Intensity);