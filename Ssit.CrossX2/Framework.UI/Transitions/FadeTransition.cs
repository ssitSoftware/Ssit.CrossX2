using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Handlers;

namespace Ssit.CrossX2.Framework.UI.Transitions;

public class FadeTransition: Transition
{
    public bool Inverse { get; set; }
    protected override void OnApply(ViewHandler handler, IRenderer renderer, float scale, float progress)
    {
        if (!Inverse)
        {
            progress = 1 - progress;
        }

        var tint = RgbaColor.White * progress;
        renderer.StateManager.Tint(tint);
    }
}