using System.Numerics;
using System.Runtime.InteropServices;

namespace Ssit.CrossX2.Framework.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct VertexPct(Vector3 position, RgbaColor color, Vector2 texCoordinates)
{
    public const VertexComponents Components = VertexComponents.Position | VertexComponents.Color | VertexComponents.Texture;

    public VertexPct(Vector2 position, RgbaColor color, Vector2 texCoordinates) : this(new Vector3(position, 0), color, texCoordinates)
    {
    }
    
    public Vector3 Position = position;
    public RgbaColor Color = color;
    public Vector2 TexCoordinates = texCoordinates;
}