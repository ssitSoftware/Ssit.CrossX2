using Ssit.CrossX2.Graphics.Font;
using Ssit.CrossX2.Input;
using Ssit.CrossX2.UI.Common.Pages;
using Ssit.CrossX2.UI.Services;
using Ssit.CrossX2.UI.Views;

namespace Ssit.CrossX2.UI.Handlers;

public class LabelRadioHandler<TLabelRadio>: LabelButtonHandler<TLabelRadio> where TLabelRadio: LabelRadio
{
    protected override bool IsChecked => (AttachedView.SelectedValue?.Value ?? -1) == AttachedView.Value;
    
    public LabelRadioHandler(CreateHandlerParameters parameters, IFontsManager fontsManager, IUiActionDispatcher uiActionDispatcher,
        IUiSounds uiSounds, IHapticDevice hapticDevice, PageInputContext pageInputContext) 
        : base(parameters, fontsManager, uiActionDispatcher, uiSounds, hapticDevice, pageInputContext)
    {
    }
}