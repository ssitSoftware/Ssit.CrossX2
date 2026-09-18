using System.Diagnostics;
using System.Numerics;
using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.Input;
using Ssit.CrossX2.UI.Common.Pages;
using Ssit.CrossX2.UI.Handlers.Helpers;
using Ssit.CrossX2.UI.Services;
using Ssit.CrossX2.UI.Values;

using Button = Ssit.CrossX2.UI.Views.Button;

namespace Ssit.CrossX2.UI.Handlers;

public class ButtonHandler : ContainerHandler<Button>, IInputConsumer, IFocusable, IColorSource
{
    private readonly IRenderer _renderer;
    private readonly PageInputContext _pageInputContext;
    private readonly ButtonHelper<Button, ButtonHandler> _buttonHelper;

    public bool Enabled => _buttonHelper.IsEnabled;
    public bool Focused { get; private set; }
    public bool DisableAllInput => _buttonHelper.IsExecutingCommand;
    public bool SkipNavigation => false;
    public string UniqueId => AttachedView?.UniqueId;

    protected override RgbaColor? BackgroundColor(IRenderer renderer) => GetColor(nameof(Button.BackgroundColors)) ?? base.BackgroundColor(renderer);
    
    public ButtonHandler(CreateHandlerParameters parameters, IHandlerMapper handlerMapper,
        IUiSounds uiSounds, IHapticDevice hapticDevice,
        IRenderer renderer,
        PageInputContext pageInputContext)
        : base(parameters, handlerMapper)
    {
        _renderer = renderer;
        _pageInputContext = pageInputContext;
        _buttonHelper = new ButtonHelper<Button, ButtonHandler>(this, AttachedView?.CustomSounds ?? uiSounds, hapticDevice, pageInputContext);
    }

    public void ProcessHover(Vector2? hoverPosition, int? matchingPointerId, IInputContext context) =>
        _buttonHelper.ProcessHover(hoverPosition, matchingPointerId, context);

    public bool ProcessInput(IReadOnlyList<Pointer> pointers, IInputContext context) =>
        _buttonHelper.ProcessInput(pointers, context);

    public void CancelPointer(int pointerId, IInputContext context) =>
        _buttonHelper.CancelPointer(pointerId, context);

    private void GetChildrenCommandHandlers(IList<IUiCommandHandler> list, IReadOnlyList<ViewHandler> children)
    {
        foreach (var child in children)
        {
            if (child is IUiCommandHandler handler)
            {
                list.Add(handler);
            }

            if (child is IChildrenContainer container)
            {
                GetChildrenCommandHandlers(list, container.Children);
            }
        }
    }
    
    public bool OnUiButton(UiButton button, IInputContext context)
    {
        var handlers = new List<IUiCommandHandler>();
        GetChildrenCommandHandlers(handlers, Children);
        
        foreach (var handler in handlers)
        {
            if (handler.OnUiButton(button, context))
                return true;
        }
        
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

    public void SetFocus() => Focused = true;

    public bool ResetFocus()
    {
        Focused = false;
        return true;
    }

    protected override void OnDispose(bool disposing)
    {
        base.OnDispose(disposing);
        _buttonHelper.Dispose();
    }

    public RgbaColor? GetColor(string id)
    {
        IButtonStateColors color = null;
        switch (id)
        {
            case nameof(Button.ForegroundColors):
                color = AttachedView?.ForegroundColors;
                break;
            
            case nameof(Button.OutlineColors):
                color = AttachedView?.OutlineColors;
                break;
            
            case nameof(Button.BackgroundColors):
                color = AttachedView?.BackgroundColors;
                break;
        }
        
        return color?.GetColor(_renderer, _buttonHelper.IsHovered, Focused && _pageInputContext.ShowFocus, _buttonHelper.IsPressed || _buttonHelper.IsExecutingCommand, Enabled, false);
    }
}
