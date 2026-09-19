using System.Numerics;

namespace Ssit.CrossX2.Graphics.Lighting;

public record struct PointLight(Vector3 Position, float Radius, RgbaColor Color, float Intensity);