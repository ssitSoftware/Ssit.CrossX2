using System.Numerics;

namespace Ssit.CrossX2.Framework.Graphics.Lighting;

public record struct DirectionalLight(Vector3 Direction, RgbaColor Color, float Intensity);