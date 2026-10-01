using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.IoC;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI.Handlers;

public class NinePatchCardHandler : ViewHandler<NinePatchCard>
{
    private readonly IIoCContainer _container;
    private readonly IColorSource _colorSource;

    public NinePatchCardHandler(CreateHandlerParameters parameters, IIoCContainer container) : base(parameters)
    {
        _container = container;
        _colorSource = parameters.Parent?.GetParent<IColorSource>(true);

        if (AttachedView.Source != null)
        {
            AttachedView.Source.ImageChanged += OnImageChanged;
        }
    }

    private void OnImageChanged() => Parent?.RecalculateLayout(AttachedView);

    protected override void OnDraw(IRenderer renderer)
    {
        base.OnDraw(renderer);

        var textureArray = AttachedView.Source?.GetImage(_container)?.Resource;
        if (textureArray is null) return;

        if (AttachedView.Filter.HasValue)
        {
            renderer.StateManager.SetTextureFilter(AttachedView.Filter.Value);
        }

        var textures = textureArray.Textures;
        var colors = AttachedView.Colors;

        for (var idx = 0; idx < textures.Length; ++idx)
        {
            var texture = textures[idx];
            if (texture is null) continue;

            var color = colors != null && idx < colors.Length ? colors[idx].GetColor(renderer, _colorSource) : null;

            renderer.SpriteRenderer.DrawNinePatch(texture, ScreenBounds, CurrentScale, AttachedView.SourceCenterRect, color);
        }
    }

    protected override void OnDispose(bool disposing)
    {
        base.OnDispose(disposing);

        if (AttachedView.Source != null)
        {
            AttachedView.Source.ImageChanged -= OnImageChanged;
            AttachedView.Source.Dispose();
        }
    }
}
