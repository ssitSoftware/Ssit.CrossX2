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

    /// <summary>
    /// Whether the glow pass draws the background's glow color. Otherwise it masks the background with black.
    /// </summary>
    protected virtual bool BackgroundGlows => AttachedView.BackgroundColor.HasGlow;
    
    protected override void OnDraw(IRenderer renderer)
    {
        var bgColor = BackgroundColor(renderer);
        if (bgColor.HasValue)
        {
            if (renderer.CurrentPass == RenderPass.Normal || bgColor.Value.A > 0)
            {
                var color = renderer.CurrentPass == RenderPass.Glow && !BackgroundGlows ? RgbaColor.Black : bgColor.Value;
                renderer.GeometryRenderer.FillRectangle(ScreenBounds, color);
            }
        }
    }
}

public class BackgroundHandler(ViewHandler.CreateHandlerParameters parameters) 
    : BackgroundHandler<Background>(parameters)
{
}