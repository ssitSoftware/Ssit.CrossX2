using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Handlers;
using Ssit.CrossX2.Framework.UI.Services;

namespace Ssit.CrossX2.Framework.UI.Transitions;

public class OnlyBottomPageTransition: Transition
{
    protected override void OnApply(ViewHandler handler, IRenderer renderer, float scale, float progress)
    {
        var page = handler.Parent.GetParent<IPage>();
        var navigation = (Navigation)page.Services.Get<INavigation>();

        if (navigation.PreviousPageOnTop)
        {
            if (navigation.CurrentPage != page)
            {
                renderer.StateManager.Tint(RgbaColor.Transparent);
            }
        }
        else if (navigation.PreviousPage != null && page != navigation.PreviousPage)
        {
            renderer.StateManager.Tint(RgbaColor.Transparent);
        }
    }
}