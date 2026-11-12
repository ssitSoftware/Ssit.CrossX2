using System.Numerics;
using System.Runtime.InteropServices;

namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// Vertex with position, color and texture coordinates (P-C-T), laid out sequentially for the GPU.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VertexPct(Vector3 position, RgbaColor color, Vector2 texCoordinates)
{
    /// <summary>
    /// Components contained in this vertex type.
    /// </summary>
    public const VertexComponents Components = VertexComponents.Position | VertexComponents.Color | VertexComponents.Texture;

    /// <summary>
    /// Creates a vertex from a 2D position and an optional depth (Z).
    /// </summary>
    public VertexPct(Vector2 position, RgbaColor color, Vector2 texCoordinates, float depth = 0) : this(new Vector3(position, depth), color, texCoordinates)
    {
    }
    
    /// <summary>
    /// Position; Z is the depth.
    /// </summary>
    public Vector3 Position = position;
    /// <summary>
    /// Vertex color multiplied with the texture.
    /// </summary>
    public RgbaColor Color = color;
    /// <summary>
    /// Texture coordinates (0..1).
    /// </summary>
    public Vector2 TexCoordinates = texCoordinates;
}