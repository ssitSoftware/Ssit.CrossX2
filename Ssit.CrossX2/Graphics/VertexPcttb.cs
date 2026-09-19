using System.Numerics;
using System.Runtime.InteropServices;

namespace Ssit.CrossX2.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct VertexPcttb(VertexPct vertex, Vector2 tangent, Vector2 binormal)
{
    public const VertexComponents Components = VertexComponents.Position | VertexComponents.Color | VertexComponents.Texture | VertexComponents.Tangent | VertexComponents.BiNormal;
    
    public Vector3 Position = vertex.Position;
    public RgbaColor Color = vertex.Color;
    public Vector2 TexCoordinates = vertex.TexCoordinates;
    public Vector2 Tangent = tangent;
    public Vector2 BiNormal = binormal;
    
    public static implicit operator VertexPcttb(VertexPct vertex) => new(vertex, Vector2.Zero, Vector2.Zero);
}