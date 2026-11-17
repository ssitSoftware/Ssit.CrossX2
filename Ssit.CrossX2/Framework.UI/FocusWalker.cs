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

    /// <summary>
    /// Focuses the element, or clears the focus when <paramref name="focusable"/> is null.
    /// Elements without a unique id can't be focused - the call is ignored.
    /// </summary>
    public void SetFocus(IFocusable focusable)
    {
        if (focusable is not null && string.IsNullOrWhiteSpace(focusable.UniqueId))
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
        var minCenterDistance = float.MaxValue;

        foreach (var focusable in _buffer)
        {
            if (!IsNavigable(focusable))
                continue;

            // Distance to the nearest point of the element, not to its center - otherwise a wide element right next
            // to the cursor loses to a narrow one further away (its center is far to the side).
            var elementBounds = focusable.ScreenBounds;
            var nearest = Vector2.Clamp(_focusCursorPosition, elementBounds.TopLeft, elementBounds.BottomRight);
            var dist = (nearest - _focusCursorPosition).Length();

            // Equally near (e.g. both overlap the cursor's column) - the closer center wins.
            var centerDist = (elementBounds.Center - _focusCursorPosition).Length();

            if (dist < minDistance || (dist == minDistance && centerDist < minCenterDistance))
            {
                minDistance = dist;
                minCenterDistance = centerDist;
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

    /// <summary>
    /// Whether <paramref name="rect"/> lies within <paramref name="bounds"/>, edges included.
    /// RectangleF.Contains excludes the right and bottom edge, so an element touching the current one from above
    /// (or from the left) - its bottom (right) equal to the search bounds' bottom (right) - would never be found.
    /// </summary>
    private static bool ContainsInclusive(RectangleF bounds, RectangleF rect) =>
        rect.X >= bounds.X && rect.Right <= bounds.Right &&
        rect.Y >= bounds.Y && rect.Bottom <= bounds.Bottom;

    private void FillWithFocusables(ViewHandler handler, List<IFocusable> buffer, RectangleF bounds)
    {
        if (!handler.View.Visible.Value)
            return;

        if (handler is IFocusable focusable && ContainsInclusive(bounds, focusable.ScreenBounds))
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