using System.Numerics;

namespace Ssit.CrossX2.Framework.Graphics.Utils;

public static class GeometryUtils
{
    public static (Vector2 Tangent, Vector2 Bitangent) CalculateTangentAndBiTangent(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 tc0, Vector2 tc1, Vector2 tc2)
    {
        var edge1 = p1 - p0;
        var edge2 = p2 - p0;

        var deltaUv1 = tc1 - tc0;
        var deltaUv2 = tc2 - tc0;

        var denom = deltaUv1.X * deltaUv2.Y - deltaUv2.X * deltaUv1.Y;
        var f = MathF.Abs(denom) > 1e-8f ? 1f / denom : 0f;

        var tangent = f * (deltaUv2.Y * edge1 - deltaUv1.Y * edge2);
        var bitangent = f * (-deltaUv2.X * edge1 + deltaUv1.X * edge2);

        return (Vector2.Normalize(tangent), Vector2.Normalize(bitangent));
    }
}