using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI.Handlers;

public class FrameHandler(ViewHandler.CreateHandlerParameters parameters) : BackgroundHandler<Frame>(parameters)
{
    private readonly IColorSource _colorSource = parameters.Parent.GetParent<IColorSource>(true);
    
    protected virtual RgbaColor? FrameColor(IRenderer renderer) => AttachedView.FrameColor.GetColor(renderer, _colorSource);
    
    protected override void OnDraw(IRenderer renderer)
    {
        base.OnDraw(renderer);
        
        var frameColor = FrameColor(renderer);
        var frameWidth = AttachedView.FrameWidth?.Calculate(CurrentScale, ScreenBounds.Width) ?? CurrentScale;
        
        if (frameColor.HasValue)
        {
            var topRect =  new RectangleF(ScreenBounds.X, ScreenBounds.Y, ScreenBounds.Width, frameWidth);
            renderer.GeometryRenderer.FillRectangle(topRect, frameColor.Value);
            
            var bottomRect = new RectangleF(ScreenBounds.X, ScreenBounds.Bottom - frameWidth, ScreenBounds.Width, frameWidth);
            renderer.GeometryRenderer.FillRectangle(bottomRect, frameColor.Value);
            
            var leftRect = new RectangleF(ScreenBounds.X, ScreenBounds.Y + frameWidth, frameWidth, ScreenBounds.Height - frameWidth * 2);
            renderer.GeometryRenderer.FillRectangle(leftRect, frameColor.Value);
            
            var rightRect = new RectangleF(ScreenBounds.Right - frameWidth, ScreenBounds.Y + frameWidth, frameWidth, ScreenBounds.Height - frameWidth * 2);
            renderer.GeometryRenderer.FillRectangle(rightRect, frameColor.Value);
        }
    }
}