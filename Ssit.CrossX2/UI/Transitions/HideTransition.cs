using Ssit.CrossX2.Graphics;

namespace Ssit.CrossX2.UI.Transitions;

public class HideTransition: Transition
{
    protected override void OnApply( IRenderer renderer, float scale, float progress)
    {
        if (progress > 0)
        {
            renderer.StateManager.Scale(0);
        }
    }
}