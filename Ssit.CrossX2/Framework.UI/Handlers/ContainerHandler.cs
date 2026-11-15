using Ssit.CrossX2.Framework.UI.Parameters;
using Ssit.CrossX2.Framework.UI.Services;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI.Handlers;

public class ContainerHandler<TContainer>(
    ViewHandler.CreateHandlerParameters parameters,
    IHandlerMapper handlerMapper) :
    ChildrenContainerHandler<TContainer>(parameters, handlerMapper) where TContainer: ChildrenContainer
{
    private float _lastAutoWidth = -1f;
    private float _lastAutoHeight = -1f;

    protected override void RecalculateChildrenLayouts()
    {
        if (RecalculateChildren.Count == 0)
            return;

        foreach (var child in RecalculateChildren)
        {
            CalculateChildPosition(child);
        }
        RecalculateChildren.Clear();

        // Propagate a size change upward only when the size is auto and actually changed.
        if (View.Width is { IsAuto: true } || View.Height is { IsAuto: true })
        {
            CalculateSize(out var width, out var height);
            var w = width.IsAuto ? 0 : width.Calculate(CurrentScale, 0);
            var h = height.IsAuto ? 0 : height.Calculate(CurrentScale, 0);

            if (MathF.Abs(w - _lastAutoWidth) > 0.5f || MathF.Abs(h - _lastAutoHeight) > 0.5f)
            {
                _lastAutoWidth = w;
                _lastAutoHeight = h;
                Parent?.RecalculateLayout(AttachedView);
            }
        }
    }

    /// <summary>
    /// Width / height set to <see cref="Length.Auto"/>: the size of the largest child plus padding.
    /// Children sized relative to the container (percent, fill) don't count - their size depends on the container.
    /// Without explicit Auto the container keeps the default size (<see cref="Length.Fill"/> when not set).
    /// </summary>
    public override void CalculateSize(out Length width, out Length height)
    {
        base.CalculateSize(out width, out height);

        if (!width.IsAuto && !height.IsAuto)
            return;

        float maxWidth = 0;
        float maxHeight = 0;

        foreach (var child in AttachedView.Children)
        {
            ((IHandlerView)child).Handler.CalculateSize(out var childWidth, out var childHeight);

            if (!childWidth.IsAuto)
            {
                maxWidth = MathF.Max(maxWidth, childWidth.Calculate(CurrentScale, 0));
            }

            if (!childHeight.IsAuto)
            {
                maxHeight = MathF.Max(maxHeight, childHeight.Calculate(CurrentScale, 0));
            }
        }

        if (width.IsAuto)
        {
            width = CalculateLengthWithPadding(new Length(pixels: maxWidth), AttachedView.Padding?.Left, AttachedView.Padding?.Right);
        }

        if (height.IsAuto)
        {
            height = CalculateLengthWithPadding(new Length(pixels: maxHeight), AttachedView.Padding?.Top, AttachedView.Padding?.Bottom);
        }
    }
    
    private void CalculateChildPosition(View child)
    {
        var handlerView = (IHandlerView)child;
        
        var x = child.AnchorX ?? Length.Auto;
        var y = child.AnchorY ?? Length.Auto;

        handlerView.Handler.CalculateSize(out var width, out var height);
        handlerView.Handler.CalculateAlign(out var horizontalAlign, out var verticalAlign);

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
        
        var xx = bounds.X + x.Calculate(CurrentScale, bounds.Width);
        var yy = bounds.Y + y.Calculate(CurrentScale, bounds.Height);
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
    }
}

public class ContainerHandler(ViewHandler.CreateHandlerParameters parameters, IHandlerMapper handlerMapper)
    : ContainerHandler<Container>(parameters, handlerMapper)
{
}