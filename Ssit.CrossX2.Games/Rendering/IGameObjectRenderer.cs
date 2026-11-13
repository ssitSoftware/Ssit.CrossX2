using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.Games.Rendering;

public interface IGameObjectRenderer
{
    int FilterMode => 0;
    int ZOrder { get; }
    RectangleF Bounds { get; }
    void Render(IRenderer renderer, RgbaColor color, float depth);
}