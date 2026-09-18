using System.Numerics;
using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.Input;
using Ssit.CrossX2.UI.Common.Pages;
using Ssit.CrossX2.UI.Services;
using Ssit.CrossX2.UI.Values;
using Ssit.CrossX2.UI.Views;

namespace Ssit.CrossX2.UI.Handlers;

public class FocusableContainerHandler(ViewHandler.CreateHandlerParameters parameters, IHandlerMapper handlerMapper, PageInputContext pageInputContext, IUiSounds uiSounds, IRenderer renderer) 
    : ContainerHandler<FocusableContainer>(parameters, handlerMapper), IFocusable,
        IInputConsumer, IColorSource
{
    protected override RgbaColor? BackgroundColor(IRenderer _) => Focused && pageInputContext.ShowFocus ? AttachedView.FocusBackgroundColor?.GetColor(renderer) ?? base.BackgroundColor(renderer) : base.BackgroundColor(renderer);
    
    public bool Enabled => !string.IsNullOrWhiteSpace(AttachedView.UniqueId);
    public bool Focused { get; private set; }
    public bool DisableAllInput => false;
    
    public RgbaColor? GetColor(string id)
    {
        if (Focused && pageInputContext.ShowFocus)
        {
            switch (id)
            {
                case nameof(FocusableContainer.FocusColor):
                    return AttachedView.FocusColor?.GetColor(renderer);
                
                case nameof(FocusableContainer.FocusOutlineColor):
                    return AttachedView.FocusOutlineColor?.GetColor(renderer);
            }
        }

        return null;
    }
    
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
        
        var focusDirection = button switch
        {
            UiButton.Up => FocusDirection.Up,
            UiButton.Down => FocusDirection.Down,
            UiButton.Left => FocusDirection.Left,
            UiButton.Right => FocusDirection.Right,
            _ => FocusDirection.None
        };

        if (focusDirection == FocusDirection.None) 
            return false;
        
        if (!pageInputContext.ShowFocus)
        {
            uiSounds[UiSounds.ItemNavigateSound]?.PlayOnce();
            pageInputContext.ShowFocus = true;
            context.Focus(this, this);
            return true;
        }

        if (context.MoveFocus(focusDirection, this))
        {
            uiSounds[UiSounds.ItemNavigateSound]?.PlayOnce();
        }

        return false;
    }

    public void SetFocus() => Focused = true;

    public bool ResetFocus()
    {
        Focused = false;
        return true;
    }

    public string UniqueId => AttachedView?.UniqueId;
    public bool SkipNavigation => false;
    
    public void ProcessHover(Vector2? hoverPosition, int? matchingPointerId, IInputContext context)
    {
    }

    public bool ProcessInput(IReadOnlyList<Pointer> pointers, IInputContext context)
    {
        return false;
    }

    public void CancelPointer(int pointerId, IInputContext context)
    {
    }
}