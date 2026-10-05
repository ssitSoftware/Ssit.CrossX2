using System.Numerics;
using System.Runtime.InteropServices;

namespace Ssit.CrossX2.Framework.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct VertexPct(Vector3 position, RgbaColor color, Vector2 texCoordinates)
{
    public const VertexComponents Components = VertexComponents.Position | VertexComponents.Color | VertexComponents.Texture;

    public VertexPct(Vector2 position, RgbaColor color, Vector2 texCoordinates, float depth = 0) : this(new Vector3(position, depth), color, texCoordinates)
    {
    }
    
    public Vector3 Position = position;
    public RgbaColor Color = color;
    public Vector2 TexCoordinates = texCoordinates;
}