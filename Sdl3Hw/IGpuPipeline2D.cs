using System.Numerics;
using Ssit.CrossX2;
using Ssit.CrossX2.Framework;

namespace Sdl3Hw;

public interface IGpuPipeline2D : IGpuPipeline
{
    Vector2 Offset { get; set; }
    float Scale { get; set; }
    Size ScreenSize { get; set; }
}