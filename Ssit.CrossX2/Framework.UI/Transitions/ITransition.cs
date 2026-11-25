using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Handlers;

namespace Ssit.CrossX2.Framework.UI.Transitions;

public interface ITransition
{
    /// <summary>
    /// Transition types the transition is applied for.
    /// </summary>
    TransitionType ForTransitions { get; }

    void Apply(ViewHandler view, IRenderer renderer, float scale, TransitionType type, float progress);
    void Finish(IRenderer renderer);
}