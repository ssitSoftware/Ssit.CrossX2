using Ssit.CrossX2.UI.Common.Pages;
using Ssit.CrossX2.UI.Services;
using Ssit.CrossX2.UI.Values;

namespace Ssit.CrossX2.UI;

public abstract class MenuPage<TViewModel>(float transitionTime = 0.1f) : PageWithTranslator<TViewModel> where TViewModel: class
{
    public override float TransitionTime => transitionTime;
    
    protected string DefaultId { get; set; }

    protected override void OnLoad(IInputContext inputContext)
    {
        if (Services.Get<PageInputContext>().ShowFocus)
        {
            var focusable = inputContext.FindFocusable(DefaultId, this);

            if (focusable is not null)
            {
                inputContext.Focus(focusable, this);
            }
        }
    }
    
    protected override bool OnUiButton(UiButton button, IInputContext inputContext)
    {
        if (FocusedElement is null && !string.IsNullOrWhiteSpace(DefaultId) && button is not UiButton.Back and not UiButton.MenuOrBack)
        {
            var focusable = inputContext.FindFocusable(DefaultId, this);
            inputContext.Focus(focusable, this);
            Services.Get<PageInputContext>().ShowFocus = true;
            return true;
        }
        return base.OnUiButton(button, inputContext);
    }
}