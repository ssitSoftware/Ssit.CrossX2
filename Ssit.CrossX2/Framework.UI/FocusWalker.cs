using System.Numerics;
using Ssit.CrossX2.Framework.UI.Handlers;
using Ssit.CrossX2.Framework.UI.Services;

namespace Ssit.CrossX2.Framework.UI;

internal class FocusWalker(IPage page)
{
    private readonly List<IFocusable> _buffer = new();

    public IFocusable FocusedElement
    {
        get;
        private set
        {
            field?.ResetFocus();
            field = value;
            field?.SetFocus();
        }
    }

    private Vector2 _focusCursorPosition = Vector2.Zero;

    public void SetFocus(IFocusable focusable)
    {
        if (string.IsNullOrWhiteSpace(focusable.UniqueId))
            return;
        
        FocusedElement = focusable;
    }
    
    public bool MoveFocus(FocusDirection direction)
    {
        if (direction is FocusDirection.None)
            return false;
        
        var current = FocusedElement;
        if (current is null) return false;

        _focusCursorPosition.X = MathF.Max(current.ScreenBounds.X, MathF.Min(current.ScreenBounds.Right, _focusCursorPosition.X));
        _focusCursorPosition.Y = MathF.Max(current.ScreenBounds.Y, MathF.Min(current.ScreenBounds.Bottom, _focusCursorPosition.Y));
        
        var bounds = page.RootHandler.ScreenBounds;
        
        switch (direction)
        {
            case FocusDirection.Up:
                bounds = new RectangleF(bounds.X, bounds.Y, bounds.Width, current.ScreenBounds.Y - bounds.Y);
                break;
            
            case FocusDirection.Down:
                bounds = new RectangleF(bounds.X, current.ScreenBounds.Bottom, bounds.Width, bounds.Bottom - current.ScreenBounds.Bottom);
                break;
            
            case FocusDirection.Left:
                bounds = new RectangleF(bounds.X, bounds.Y, current.ScreenBounds.X - bounds.X, bounds.Height);
                break;
            
            case FocusDirection.Right:
                bounds = new RectangleF(current.ScreenBounds.Right, bounds.Y, bounds.Right - current.ScreenBounds.Right, bounds.Height);
                break;
        }

        _buffer.Clear();
        FillWithFocusables(page.RootHandler, _buffer, bounds);

        IFocusable newFocusable = null;
        
        float minDistance = float.MaxValue;
        foreach (var focusable in _buffer)
        {
            if (!focusable.Enabled)
                continue;
            
            if (focusable.SkipNavigation)
                continue;

            if (string.IsNullOrWhiteSpace(focusable.UniqueId))
                continue;
            
            var center = focusable.ScreenBounds.Center;
            var dist = (center - _focusCursorPosition).Length();
            
            if ( dist < minDistance)
            {
                minDistance = dist;
                newFocusable = focusable;
            }
        }

        if (newFocusable != null)
        {
            FocusedElement = newFocusable;

            if (direction is FocusDirection.Down or FocusDirection.Up)
            {
                _focusCursorPosition.Y = FocusedElement.ScreenBounds.Center.Y;
                _focusCursorPosition.X = MathF.Max(FocusedElement.ScreenBounds.X, MathF.Min(FocusedElement.ScreenBounds.Right, _focusCursorPosition.X));
            }
            else if (direction is FocusDirection.Left or FocusDirection.Right)
            {
                _focusCursorPosition.X = FocusedElement.ScreenBounds.Center.X;
                _focusCursorPosition.Y = MathF.Max(FocusedElement.ScreenBounds.Y, MathF.Min(FocusedElement.ScreenBounds.Bottom, _focusCursorPosition.Y));
            }

            return true;
        }

        return false;
    }

    private void FillWithFocusables(ViewHandler handler, List<IFocusable> buffer, RectangleF bounds)
    {
        if (handler is IFocusable focusable && bounds.Contains(focusable.ScreenBounds.TopLeft) && bounds.Contains(focusable.ScreenBounds.BottomRight))
        {
            buffer.Add(focusable);
        }
        
        if (handler is IChildrenContainer container)
        {
            foreach (var child in container.Children ?? [])
            {
                FillWithFocusables(child, buffer, bounds);
            }
        }
    }
}