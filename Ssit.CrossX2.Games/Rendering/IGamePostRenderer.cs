using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.Games.Rendering;

public interface IGamePostRenderer
{
    int FilterMode => 0;
    void Render(IRenderer renderer);
}