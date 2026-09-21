using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.UI.Transitions;

public interface ITransition
{
    void Apply(IRenderer renderer2, float scale, TransitionType type, float progress);
    void Finish(IRenderer renderer2);
}