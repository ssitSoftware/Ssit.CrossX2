using System.Numerics;

namespace Ssit.CrossX2.Graphics.Lighting;

public record struct SpotLight(Vector3 Position, Vector2 Direction, float OuterAngle, float InnerAngle, float Radius, RgbaColor Color, float Intensity);