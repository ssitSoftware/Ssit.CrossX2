using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.Games.Rendering;

public interface IGameObjectRenderer
{
    int ZOrder { get; }
    RectangleF Bounds { get; }
    void Render(IRenderer renderer, RgbaColor color);
}