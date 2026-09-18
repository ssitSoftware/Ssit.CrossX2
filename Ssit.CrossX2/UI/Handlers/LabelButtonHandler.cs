using System.Diagnostics;
using System.Numerics;
using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.Graphics.Font;
using Ssit.CrossX2.Input;
using Ssit.CrossX2.UI.Common.Pages;
using Ssit.CrossX2.UI.Handlers.Helpers;
using Ssit.CrossX2.UI.Services;
using Ssit.CrossX2.UI.Values;
using Ssit.CrossX2.UI.Views;

namespace Ssit.CrossX2.UI.Handlers;

public class LabelButtonHandler<TLabelButton>: LabelHandler<TLabelButton>, IInputConsumer, IFocusable where TLabelButton: LabelButton
{
    private readonly PageInputContext _pageInputContext;
    protected override RgbaColor? BackgroundColor(IRenderer renderer) => AttachedView.BackgroundColors?.GetColor(renderer, _buttonHelper.IsHovered, Focused && _pageInputContext.ShowFocus, _buttonHelper.IsPressed || _buttonHelper.IsExecutingCommand, Enabled, IsChecked);
    protected override RgbaColor? TextColor(IRenderer renderer, bool? focused = null) => AttachedView.TextColors?.GetColor(renderer, _buttonHelper.IsHovered, (focused ?? Focused) && _pageInputContext.ShowFocus, _buttonHelper.IsPressed || _buttonHelper.IsExecutingCommand, Enabled, IsChecked);
    protected override RgbaColor? TextOutlineColor(IRenderer renderer) => AttachedView.TextOutlineColors?.GetColor(renderer, _buttonHelper.IsHovered, Focused && _pageInputContext.ShowFocus, _buttonHelper.IsPressed || _buttonHelper.IsExecutingCommand, Enabled, IsChecked);
 
    protected virtual bool IsChecked => false;
    
    public bool Enabled => _buttonHelper.IsEnabled;
    public bool DisableAllInput => _buttonHelper.IsExecutingCommand;
    
    public bool Focused { get; private set; }
    
    public bool IsPushed => _buttonHelper.IsPressed;

    public bool SkipNavigation => false;

    private readonly ButtonHelper<TLabelButton, LabelButtonHandler<TLabelButton>> _buttonHelper;

    public LabelButtonHandler(CreateHandlerParameters parameters, IFontsManager fontsManager, 
        IUiActionDispatcher uiActionDispatcher, IUiSounds uiSounds, IHapticDevice hapticDevice, 
        PageInputContext pageInputContext) 
        : base(parameters, fontsManager, uiActionDispatcher)
    {
        _pageInputContext = pageInputContext;
        _buttonHelper = new ButtonHelper<TLabelButton, LabelButtonHandler<TLabelButton>>(this, AttachedView?.CustomSounds ?? uiSounds, hapticDevice, pageInputContext);
    }

    public void ProcessHover(Vector2? hoverPosition, int? matchingPointerId, IInputContext context) => _buttonHelper.ProcessHover(hoverPosition, matchingPointerId, context);

    public bool ProcessInput(IReadOnlyList<Pointer> pointer, IInputContext context) => _buttonHelper.ProcessInput(pointer, context);

    public void CancelPointer(int pointerId, IInputContext context) => _buttonHelper.CancelPointer(pointerId, context);

    public bool OnUiButton(UiButton button, IInputContext context)
    {
        try
        {
            return _buttonHelper.OnUiButton(button, context);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            Debugger.Break();
        }

        return false;
    }

    public void SetFocus()
    {
        Focused = true;
    }

    public bool ResetFocus()
    {
        Focused = false;
        return true;
    }

    public string UniqueId => AttachedView?.UniqueId;

    protected override void OnDispose(bool disposing)
    {
        base.OnDispose(disposing);
        _buttonHelper.Dispose();
    }
}   