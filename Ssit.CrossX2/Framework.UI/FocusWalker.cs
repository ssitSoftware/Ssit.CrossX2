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
        
        var rootBounds = page.RootHandler.ScreenBounds;
        var bounds = rootBounds;

        switch (direction)
        {
            case FocusDirection.Up:
                if (!page.FocusNavigationMode.HasFlag(FocusNavigationMode.UpDown))
                    return false;
                
                bounds = new RectangleF(bounds.X, bounds.Y, bounds.Width, current.ScreenBounds.Y - bounds.Y);
                break;

            case FocusDirection.Down:
                if (!page.FocusNavigationMode.HasFlag(FocusNavigationMode.UpDown))
                    return false;
                
                bounds = new RectangleF(bounds.X, current.ScreenBounds.Bottom, bounds.Width, bounds.Bottom - current.ScreenBounds.Bottom);
                break;

            case FocusDirection.Left:
                if (!page.FocusNavigationMode.HasFlag(FocusNavigationMode.LeftRight))
                    return false;
                
                bounds = new RectangleF(bounds.X, bounds.Y, current.ScreenBounds.X - bounds.X, bounds.Height);
                break;

            case FocusDirection.Right:
                if (!page.FocusNavigationMode.HasFlag(FocusNavigationMode.LeftRight))
                    return false;
                
                bounds = new RectangleF(current.ScreenBounds.Right, bounds.Y, bounds.Right - current.ScreenBounds.Right, bounds.Height);
                break;
        }

        var newFocusable = FindClosestFocusable(bounds);

        if (newFocusable is null && page.FocusNavigationMode.HasFlag(FocusNavigationMode.Wrap))
        {
            newFocusable = FindWrappedFocusable(rootBounds, direction, current);
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

    private IFocusable FindClosestFocusable(RectangleF bounds)
    {
        _buffer.Clear();
        FillWithFocusables(page.RootHandler, _buffer, bounds);

        IFocusable result = null;
        var minDistance = float.MaxValue;

        foreach (var focusable in _buffer)
        {
            if (!IsNavigable(focusable))
                continue;

            var dist = (focusable.ScreenBounds.Center - _focusCursorPosition).Length();

            if (dist < minDistance)
            {
                minDistance = dist;
                result = focusable;
            }
        }

        return result;
    }

    private IFocusable FindWrappedFocusable(RectangleF rootBounds, FocusDirection direction, IFocusable current)
    {
        _buffer.Clear();
        FillWithFocusables(page.RootHandler, _buffer, rootBounds);

        IFocusable result = null;
        var bestPrimary = float.MaxValue;
        var bestSecondary = float.MaxValue;

        foreach (var focusable in _buffer)
        {
            if (focusable == current || !IsNavigable(focusable))
                continue;

            var (primary, secondary) = direction switch
            {
                FocusDirection.Down => (focusable.ScreenBounds.Y, MathF.Abs(focusable.ScreenBounds.Center.X - _focusCursorPosition.X)),
                FocusDirection.Up => (-focusable.ScreenBounds.Bottom, MathF.Abs(focusable.ScreenBounds.Center.X - _focusCursorPosition.X)),
                FocusDirection.Right => (focusable.ScreenBounds.X, MathF.Abs(focusable.ScreenBounds.Center.Y - _focusCursorPosition.Y)),
                FocusDirection.Left => (-focusable.ScreenBounds.Right, MathF.Abs(focusable.ScreenBounds.Center.Y - _focusCursorPosition.Y)),
                _ => (float.MaxValue, float.MaxValue)
            };

            if (primary < bestPrimary || (primary == bestPrimary && secondary < bestSecondary))
            {
                bestPrimary = primary;
                bestSecondary = secondary;
                result = focusable;
            }
        }

        return result;
    }

    private static bool IsNavigable(IFocusable focusable) =>
        focusable.Enabled && !focusable.SkipNavigation && !string.IsNullOrWhiteSpace(focusable.UniqueId);

    private void FillWithFocusables(ViewHandler handler, List<IFocusable> buffer, RectangleF bounds)
    {
        if (!handler.View.Visible.Value)
            return;

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