using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Handlers;

namespace Ssit.CrossX2.Framework.UI.Transitions;

public class HideTransition: Transition
{
    protected override void OnApply(ViewHandler _, IRenderer renderer, float scale, float progress)
    {
        if (progress > 0)
        {
            renderer.StateManager.Scale(0);
        }
    }
}