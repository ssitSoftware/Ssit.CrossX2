using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI.Handlers;

public class BackgroundHandler<TBackground>(
    ViewHandler.CreateHandlerParameters parameters)
    : ViewHandler<TBackground>(parameters)
    where TBackground : Background
{
    private readonly IColorSource _colorSource = parameters.Parent?.GetParent<IColorSource>(true);

    protected virtual RgbaColor? BackgroundColor(IRenderer renderer) => AttachedView.BackgroundColor.GetColor(renderer, _colorSource);
    
    protected override void OnDraw(IRenderer renderer)
    {
        var bgColor = BackgroundColor(renderer);
        if (bgColor.HasValue)
        {
            if (renderer.CurrentPass == RenderPass.Normal || bgColor.Value.A > 0)
            {
                renderer.GeometryRenderer.FillRectangle(ScreenBounds, renderer.CurrentPass == RenderPass.Glow ? RgbaColor.Black : bgColor.Value);
            }
        }
    }
}

public class BackgroundHandler(ViewHandler.CreateHandlerParameters parameters) 
    : BackgroundHandler<Background>(parameters)
{
}