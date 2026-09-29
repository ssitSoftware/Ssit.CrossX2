using Ssit.CrossX2.Framework.Graphics.Font;
using Ssit.CrossX2.Framework.Input;
using Ssit.CrossX2.Framework.UI.Common.Pages;
using Ssit.CrossX2.Framework.UI.Services;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI.Handlers;

public class LabelRadioHandler<TLabelRadio>: LabelButtonHandler<TLabelRadio> where TLabelRadio: LabelRadio
{
    protected override bool IsChecked => (AttachedView.SelectedValue?.Value ?? -1) == AttachedView.Value;
    
    public LabelRadioHandler(CreateHandlerParameters parameters, IFontsManager fontsManager, IUiActionDispatcher uiActionDispatcher,
        IUiSounds uiSounds, IHapticDevice hapticDevice, IPointingDevices pointingDevices, PageInputContext pageInputContext) 
        : base(parameters, fontsManager, uiActionDispatcher, uiSounds, hapticDevice, pointingDevices, pageInputContext)
    {
    }
}