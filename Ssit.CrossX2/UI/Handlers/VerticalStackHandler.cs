using Ssit.CrossX2.UI.Parameters;
using Ssit.CrossX2.UI.Services;
using Ssit.CrossX2.UI.Views;

namespace Ssit.CrossX2.UI.Handlers;

public class VerticalStackHandler<TVerticalStack>(ViewHandler.CreateHandlerParameters parameters, IHandlerMapper handlerMapper)
    : ChildrenContainerHandler<TVerticalStack>(parameters, handlerMapper) where TVerticalStack: VerticalStack
{
    private float _lastTotalHeight = -1f;

    protected override void RecalculateChildrenLayouts()
    {
        if (RecalculateChildren.Count == 0)
            return;

        // Iterate in visual order (not HashSet order) so offsetY accumulates correctly.
        var offsetY = 0f;
        foreach (var child in AttachedView.Children)
        {
            CalculateChildPosition(child, ref offsetY);
        }
        RecalculateChildren.Clear();

        // Propagate height change upward only when our height is auto and actually changed.
        if ((View.Height == null || View.Height.Value.IsAuto) && MathF.Abs(offsetY - _lastTotalHeight) > 0.5f)
        {
            _lastTotalHeight = offsetY;
            Parent?.RecalculateLayout(AttachedView);
        }
    }

    public override void CalculateSize(out Length width, out Length height)
    {
        width = View.Width ?? Length.Auto;
        height = View.Height ?? Length.Auto;
        
        if (height.IsAuto || width.IsAuto)
        {
            float h = 0;
            float maxW = 0;

            foreach (var child in AttachedView.Children)
            {
                var handlerView = (IHandlerView)child;
                handlerView.Handler.CalculateSize(out var w, out var h1);

                h += h1.Calculate(CurrentScale, 0);
                maxW = MathF.Max(maxW, w.Calculate(CurrentScale, 0));
            }

            if (height.IsAuto)
            {
                var spaces = AttachedView.Children.Count - 1;
                h += AttachedView?.Spacing?.Calculate(CurrentScale, 0) * spaces ?? 0;
                
                height = new Length(pixels: h);
                height = CalculateLengthWithPadding(height, AttachedView!.Padding?.Top, AttachedView.Padding?.Bottom);
            }

            if (width.IsAuto)
            {
                width = new Length(pixels: maxW);
                width = CalculateLengthWithPadding(width, AttachedView.Padding?.Left, AttachedView.Padding?.Right);
            }
        }
    }

    private void CalculateChildPosition(View child, ref float offsetY)
    {
        var handlerView = (IHandlerView)child;
        
        var x = child.AnchorX ?? Length.Auto;

        handlerView.Handler.CalculateSize(out var width, out var height);
        handlerView.Handler.CalculateAlign(out var horizontalAlign, out var _);

        var bounds = CalculateTargetBounds(Bounds);

        if (x.IsAuto)
        {
            switch (horizontalAlign)
            {
                case Align.End:
                    x = Length.Fill;
                    break;
                
                case Align.Center:
                    x = new Length(0, 0.5f);
                    break;
                
                case Align.Fill:
                    x = Length.Zero;
                    break;
            }
        }
        
        var xx = bounds.X + x.Calculate(CurrentScale, bounds.Width);
        var yy = bounds.Y + offsetY;
        var ww = width.Calculate(CurrentScale, bounds.Width);
        var hh = height.Calculate(CurrentScale, bounds.Height);
        
        switch (horizontalAlign)
        {
            case Align.Fill:
                ww = bounds.Width;
                break;
            
            case Align.Start:
                break;
            
            case Align.Center:
                xx -= ww / 2f;
                break;
            
            case Align.End:
                xx -= ww;
                break;
        }
        
        handlerView.Handler.SetBounds(new RectangleF(xx, yy, ww, hh));
        offsetY += hh;
        offsetY += AttachedView?.Spacing?.Calculate(CurrentScale, 0) ?? 0;
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