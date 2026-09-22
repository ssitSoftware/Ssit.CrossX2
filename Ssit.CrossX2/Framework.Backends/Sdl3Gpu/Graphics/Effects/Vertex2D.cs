using System.Numerics;
using System.Runtime.InteropServices;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics.Effects;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex2D(Vector2 position, Vector2 texCoordinates, RgbaColor color)
{
    public Vector2 Position = position;
    public Vector2 TexCoordinates = texCoordinates;
    public RgbaColor Color = color;
}
