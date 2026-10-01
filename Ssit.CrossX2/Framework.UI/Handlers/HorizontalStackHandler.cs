using Ssit.CrossX2.Framework.UI.Parameters;
using Ssit.CrossX2.Framework.UI.Services;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI.Handlers;

public class HorizontalStackHandler<THorizontalStack>(ViewHandler.CreateHandlerParameters parameters, IHandlerMapper handlerMapper)
    : ChildrenContainerHandler<THorizontalStack>(parameters, handlerMapper) where THorizontalStack: HorizontalStack
{
    private float _lastTotalWidth = -1f;

    protected override void RecalculateChildrenLayouts()
    {
        if (RecalculateChildren.Count == 0)
            return;

        // Iterate in visual order (not HashSet order) so offsetX accumulates correctly.
        var offsetX = 0f;
        foreach (var child in AttachedView.Children)
        {
            CalculateChildPosition(child, ref offsetX);
        }
        RecalculateChildren.Clear();

        // Propagate width change upward only when our width is auto and actually changed.
        if ((View.Width == null || View.Width.Value.IsAuto) && MathF.Abs(offsetX - _lastTotalWidth) > 0.5f)
        {
            _lastTotalWidth = offsetX;
            Parent?.RecalculateLayout(AttachedView);
        }
    }

    public override void CalculateSize(out Length width, out Length height)
    {
        width = View.Width ?? Length.Auto;
        height = View.Height ?? Length.Auto;

        if (height.IsAuto || width.IsAuto)
        {
            float w = 0;
            float maxH = 0;

            foreach (var child in AttachedView.Children)
            {
                var handlerView = (IHandlerView)child;
                handlerView.Handler.CalculateSize(out var w1, out var h);

                w += w1.Calculate(CurrentScale, 0);
                maxH = MathF.Max(maxH, h.Calculate(CurrentScale, 0));
            }

            if (width.IsAuto)
            {
                var spaces = AttachedView.Children.Count - 1;
                w += AttachedView?.Spacing?.Calculate(CurrentScale, 0) * spaces ?? 0;

                width = new Length(pixels: w);
                width = CalculateLengthWithPadding(width, AttachedView!.Padding?.Left, AttachedView.Padding?.Right);
            }

            if (height.IsAuto)
            {
                height = new Length(pixels: maxH);
                height = CalculateLengthWithPadding(height, AttachedView.Padding?.Top, AttachedView.Padding?.Bottom);
            }
        }
    }

    private void CalculateChildPosition(View child, ref float offsetX)
    {
        var handlerView = (IHandlerView)child;

        var y = child.AnchorY ?? Length.Auto;

        handlerView.Handler.CalculateSize(out var width, out var height);
        handlerView.Handler.CalculateAlign(out var _, out var verticalAlign);

        var bounds = CalculateTargetBounds(Bounds);

        if (y.IsAuto)
        {
            switch (verticalAlign)
            {
                case Align.End:
                    y = Length.Fill;
                    break;

                case Align.Center:
                    y = new Length(0, 0.5f);
                    break;

                case Align.Fill:
                    y = Length.Zero;
                    break;
            }
        }

        var xx = bounds.X + offsetX;
        var yy = bounds.Y + y.Calculate(CurrentScale, bounds.Height);
        var ww = width.Calculate(CurrentScale, bounds.Width);
        var hh = height.Calculate(CurrentScale, bounds.Height);

        switch (verticalAlign)
        {
            case Align.Fill:
                hh = bounds.Height;
                break;

            case Align.Start:
                break;

            case Align.Center:
                yy -= hh / 2f;
                break;

            case Align.End:
                yy -= hh;
                break;
        }

        handlerView.Handler.SetBounds(new RectangleF(xx, yy, ww, hh));
        offsetX += ww;
        offsetX += AttachedView?.Spacing?.Calculate(CurrentScale, 0) ?? 0;
    }

    public override void RecalculateLayout(View view = null)
    {
        RecalculateChildren.UnionWith(AttachedView.Children);

        foreach (var child in RecalculateChildren)
        {
            if (child is IViewParent parent)
            {
                parent.RecalculateLayout();
            }
        }
    }
}
