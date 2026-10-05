using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Handlers;

namespace Ssit.CrossX2.Framework.UI.Transitions;

public interface ITransition
{
    void Apply(ViewHandler view, IRenderer renderer, float scale, TransitionType type, float progress);
    void Finish(IRenderer renderer);
}