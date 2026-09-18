using System.Numerics;
using System.Runtime.InteropServices;
using Ssit.CrossX2;

namespace Sdl3Hw;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex2D(Vector2 position, Vector2 texCoordinates, RgbaColor color)
{
    public Vector2 Position = position;
    public Vector2 TexCoordinates = texCoordinates;
    public RgbaColor Color = color;
}
