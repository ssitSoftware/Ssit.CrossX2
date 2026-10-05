using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Handlers;

namespace Ssit.CrossX2.Framework.UI.Transitions;

public class FadeTransition: Transition
{
    protected override void OnApply(ViewHandler handler, IRenderer renderer, float scale, float progress)
    {
        progress = 1 - progress;

        var tint = RgbaColor.White * progress;
        renderer.StateManager.Tint(tint);
    }
}