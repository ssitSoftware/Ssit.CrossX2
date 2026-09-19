using System.Numerics;
using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.IoC;
using Ssit.CrossX2.UI.Parameters;
using Ssit.CrossX2.UI.Values;
using ImageView = Ssit.CrossX2.UI.Views.ImageView;

namespace Ssit.CrossX2.UI.Handlers;

public class ImageViewHandler : BackgroundHandler<ImageView>
{
    private readonly IIoCContainer _container;

    private ImageScalingMode ScalingMode => AttachedView.Scaling ?? ImageScalingMode.None;
    
    public ImageViewHandler(CreateHandlerParameters parameters, IIoCContainer container) : base(parameters)
    {
        if (AttachedView.Source is null)
        {
            throw new InvalidOperationException("The image view is missing a Source.");
        }

        _container = container;

        AttachedView.Source.ImageChanged += OnImageChanged;
    }

    private void OnImageChanged()
    {
        Parent?.RecalculateLayout(AttachedView);
    }

    public override void CalculateAlign(out Align horizontalAlign, out Align verticalAlign)
    {
        if (AttachedView.HorizontalAlign.HasValue)
        {
            horizontalAlign = AttachedView.HorizontalAlign.Value;
        }
        else
        {
            switch (ScalingMode)
            {
                case ImageScalingMode.None:
                    horizontalAlign = Align.Center;
                    break;
                
                default:
                    horizontalAlign = Align.Fill;
                    break;
            }
        }
        
        if (AttachedView.VerticalAlign.HasValue)
        {
            verticalAlign = AttachedView.VerticalAlign.Value;
        }
        else
        {
            switch (ScalingMode)
            {
                case ImageScalingMode.None:
                    verticalAlign = Align.Center;
                    break;
                
                default:
                    verticalAlign = Align.Fill;
                    break;
            }
        }
    }

    public override void CalculateSize(out Length width, out Length height)
    {
        width = AttachedView.Width ?? Length.Auto;
        height = AttachedView.Height ?? Length.Auto;

        if (width.IsAuto || height.IsAuto)
        {
            var texture = AttachedView.Source?.GetImage(_container);
            var size = AttachedView.Source?.SourceRect?.Size ?? texture?.Resource.Size;
            
            if (width.IsAuto)
            {
                width = size?.Width ?? 0;
            }

            if (height.IsAuto)
            {
                height = size?.Height ?? 0;
            }
        }
    }

    protected override void OnDispose(bool disposing)
    {
        base.OnDispose(disposing);

        if (AttachedView.Source != null)
        {
            AttachedView.Source.ImageChanged += OnImageChanged;
            AttachedView.Source.Dispose();
        }
    }

    protected override void OnDraw(IRenderer renderer)
    {
        base.OnDraw(renderer);
        var texture = AttachedView.Source?.GetImage(_container);
        
        if (texture is null)
        {
            return;
        }
        
        var source = AttachedView.Source.SourceRect;

        CalculateTargetRects(texture.Resource, source, out var targetRect, out var sourceRect);
        renderer.SpriteRenderer.Draw(texture.Resource, targetRect, sourceRect, null, 0, AttachedView?.TintColor, AttachedView?.Transform ?? ImageTransform.None);
    }

    private void CalculateTargetRects(ITexture texture, Rectangle? source, out RectangleF targetRect, out RectangleF sourceRect)
    {
        var size = source?.Size ?? texture.Size;

        var targetSize = new SizeF(size.Width, size.Height);
        
        switch (AttachedView.Scaling)
        {
            case ImageScalingMode.AspectFit:
            {
                var sb = ScreenBounds;
                var scale = MathF.Min(sb.Width / size.Width, sb.Height / size.Height);

                var width = size.Width * scale;
                var height = size.Height * scale;

                targetSize = new SizeF(width, height);
            }
            break;
            
            case ImageScalingMode.AspectFill:
            {
                var sb = ScreenBounds;
                var scale = MathF.Max(sb.Width / size.Width, sb.Height / size.Height);

                var width = size.Width * scale;
                var height = size.Height * scale;
                
                targetSize = new SizeF(width, height);
            }
            break;
            
            case ImageScalingMode.Fill:
                targetSize = ScreenBounds.Size;
                break;
            
             case ImageScalingMode.None:
                targetSize = new SizeF(size.Width, size.Height) * CurrentScale;
                break;
        }
        
        targetRect = ScreenBounds;
        var boundsSize = targetRect.Size;
        
        Vector2 pos = Vector2.Zero;
        
        var ca = AttachedView?.ContentAlign ?? ContentAlign.Center | ContentAlign.VCenter;
        
        switch (ca & (ContentAlign.Center | ContentAlign.Right))
        {
            case ContentAlign.Center:
                pos.X += (targetRect.Width - targetSize.Width) / 2;
                break;
            
            case ContentAlign.Right:
                pos.X += targetRect.Width - targetSize.Width;
                break;
        }
        
        switch (ca & (ContentAlign.VCenter | ContentAlign.Bottom))
        {
            case ContentAlign.VCenter:
                pos.Y += (targetRect.Height - targetSize.Height) / 2;
                break;
            
            case ContentAlign.Bottom:
                pos.Y += targetRect.Height - targetSize.Height;
                break;
        }

        targetRect = new RectangleF(pos, targetSize);

        var tw = targetRect.Width + MathF.Min(targetRect.X, 0);
        tw = MathF.Min(tw, boundsSize.Width);
        
        var th = targetRect.Height + MathF.Min(targetRect.Y, 0);
        th = MathF.Min(th, boundsSize.Height);
        
        targetRect = new RectangleF(MathF.Max(targetRect.X, 0), MathF.Max(targetRect.Y, 0), tw, th);
        
        float scaleX = targetSize.Width / size.Width;
        float scaleY = targetSize.Height / size.Height;

        var offX = MathF.Max(0, -targetRect.X);
        var offY = MathF.Max(0, -targetRect.Y);
        
        sourceRect = new Rectangle( (int)(offX / scaleX), (int)(offY / scaleY), (int)(targetRect.Width / scaleX), (int)(targetRect.Height / scaleY));

        if (source.HasValue)
        {
            var src = source.Value;
            sourceRect = new RectangleF(sourceRect.X + src.X, sourceRect.Y + src.Y, sourceRect.Width, sourceRect.Height);
        }

        targetRect = targetRect.Offset(ScreenBounds.TopLeft);
    }
}