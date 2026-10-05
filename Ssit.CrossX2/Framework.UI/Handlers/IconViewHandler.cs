using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Parameters;
using Ssit.CrossX2.Framework.UI.Values;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI.Handlers;

public class IconViewHandler : ViewHandler<IconView>
{
    private readonly ResourceHandle<DualTexture> _texture;
    private readonly IColorSource _colorSource;

    private ImageScalingMode ScalingMode => AttachedView.Scaling ?? ImageScalingMode.None;

    public IconViewHandler(CreateHandlerParameters parameters, IContentManager contentManager) : base(parameters)
    {
        _texture = contentManager.Get<DualTexture>(AttachedView.IconPath);
        _colorSource = parameters.Parent?.GetParent<IColorSource>(true);
    }

    public override void CalculateAlign(out Align horizontalAlign, out Align verticalAlign)
    {
        horizontalAlign = AttachedView.HorizontalAlign ?? (ScalingMode == ImageScalingMode.None ? Align.Center : Align.Fill);
        verticalAlign = AttachedView.VerticalAlign ?? (ScalingMode == ImageScalingMode.None ? Align.Center : Align.Fill);
    }

    public override void CalculateSize(out Length width, out Length height)
    {
        var size = _texture.Resource.Texture.Size;

        width = AttachedView.Width ?? new Length(pixels: (int)(size.Width * AttachedView.Scale));
        height = AttachedView.Height ?? new Length(pixels: (int)(size.Height * AttachedView.Scale));
    }

    protected override void OnDraw(IRenderer renderer)
    {
        var fgColor = AttachedView.ForegroundColor?.GetColor(renderer, _colorSource) ?? RgbaColor.White;
        var outlineColor = AttachedView.OutlineColor?.GetColor(renderer, _colorSource) ?? RgbaColor.Black;

        var targetRect = CalculateTargetRect();

        renderer.SpriteRenderer.Draw(_texture.Resource.Texture, targetRect, nullableColor: fgColor);
        renderer.SpriteRenderer.Draw(_texture.Resource.Outline, targetRect, nullableColor: outlineColor);
    }

    private RectangleF CalculateTargetRect()
    {
        var sb = ScreenBounds;
        var size = _texture.Resource.Texture.Size;
        var targetSize = new SizeF(size.Width, size.Height) * AttachedView.Scale;

        switch (ScalingMode)
        {
            case ImageScalingMode.AspectFit:
            {
                var scale = MathF.Min(sb.Width / targetSize.Width, sb.Height / targetSize.Height);
                targetSize *= scale;
                break;
            }

            case ImageScalingMode.AspectFill:
            {
                var scale = MathF.Max(sb.Width / targetSize.Width, sb.Height / targetSize.Height);
                targetSize *= scale;
                break;
            }

            case ImageScalingMode.Fill:
                targetSize = sb.Size;
                break;

            case ImageScalingMode.None:
                targetSize *= CurrentScale;
                break;
        }

        var pos = sb.TopLeft;
        var ca = AttachedView.ContentAlign ?? ContentAlign.Center | ContentAlign.VCenter;

        switch (ca & (ContentAlign.Center | ContentAlign.Right))
        {
            case ContentAlign.Center:
                pos.X += (sb.Width - targetSize.Width) / 2;
                break;

            case ContentAlign.Right:
                pos.X += sb.Width - targetSize.Width;
                break;
        }

        switch (ca & (ContentAlign.VCenter | ContentAlign.Bottom))
        {
            case ContentAlign.VCenter:
                pos.Y += (sb.Height - targetSize.Height) / 2;
                break;

            case ContentAlign.Bottom:
                pos.Y += sb.Height - targetSize.Height;
                break;
        }

        return new RectangleF(pos, targetSize);
    }

    protected override void OnDispose(bool disposing)
    {
        base.OnDispose(disposing);
        _texture?.Dispose();
    }
}
