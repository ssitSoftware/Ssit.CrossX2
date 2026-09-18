using System.Numerics;
using System.Runtime.InteropServices;

namespace Ssit.CrossX2.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct VertexPct2D(Vector2 position, RgbaColor color, Vector2 texCoordinates)
{
    public const VertexComponents Components = VertexComponents.Position | VertexComponents.Color | VertexComponents.Texture;
    
    public Vector2 Position = position;
    public RgbaColor Color = color;
    public Vector2 TexCoordinates = texCoordinates;
}

[StructLayout(LayoutKind.Sequential)]
public struct VertexPc2D(Vector2 position, RgbaColor color)
{
    public const VertexComponents Components = VertexComponents.Position | VertexComponents.Color;
    
    public Vector2 Position = position;
    public RgbaColor Color = color;
}