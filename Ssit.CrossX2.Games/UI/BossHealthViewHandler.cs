using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.IoC;
using Ssit.CrossX2.Framework.UI.Handlers;
using Ssit.CrossX2.Framework.UI.Parameters;

namespace Ssit.CrossX2.Framework.Games.UI;

internal class BossHealthViewHandler: BackgroundHandler<BossHealthView>
{
    private readonly IIoCContainer _container;

    public BossHealthViewHandler(CreateHandlerParameters parameters, IIoCContainer container) : base(parameters)
    {
        if (AttachedView.Source is null)
        {
            throw new InvalidOperationException("BossHealthView is missing a Source.");
        }

        if (AttachedView.FillInset < 0)
        {
            throw new InvalidOperationException("BossHealthView.FillInset must not be negative.");
        }

        _container = container;

        AttachedView.Source.ImageChanged += OnImageChanged;
    }

    private void OnImageChanged()
    {
        Parent?.RecalculateLayout(AttachedView);
    }

    /// <summary>
    /// Region of the texture holding both halves (base + fill): the source rect, or the whole texture.
    /// Returns false while the image is not loaded yet.
    /// </summary>
    private bool TryGetImage(out ITexture texture, out Rectangle region)
    {
        var handle = AttachedView.Source?.GetImage(_container);
        texture = handle?.Resource;

        if (texture is null)
        {
            region = default;
            return false;
        }

        region = AttachedView.Source.SourceRect
                 ?? new Rectangle(0, 0, texture.Size.Width, texture.Size.Height);

        return region.Width > 0 && region.Height >= 2;
    }

    public override void CalculateAlign(out Align horizontalAlign, out Align verticalAlign)
    {
        horizontalAlign = AttachedView.HorizontalAlign ?? Align.Center;
        verticalAlign = AttachedView.VerticalAlign ?? Align.Start;
    }

    public override void CalculateSize(out Length width, out Length height)
    {
        CalculateAlign(out var horizontalAlign, out var verticalAlign);

        width = AttachedView.Width ?? Length.Auto;
        height = AttachedView.Height ?? Length.Auto;

        if (!width.IsAuto && !height.IsAuto) return;

        // Image not loaded yet - OnImageChanged recalculates the layout once it is.
        var loaded = TryGetImage(out _, out var region);

        if (width.IsAuto)
        {
            width = horizontalAlign == Align.Fill
                ? Length.Fill
                : loaded ? (Length)region.Width : Length.Zero;
        }

        if (height.IsAuto)
        {
            // Bar height is half of the image height.
            height = verticalAlign == Align.Fill
                ? Length.Fill
                : loaded ? (Length)(region.Height / 2) : Length.Zero;
        }
    }

    protected override void OnDraw(IRenderer renderer)
    {
        // Boss HP is UI – it must never be affected by scene lighting.
        renderer.LightingManager.EnableLighting(false);

        base.OnDraw(renderer);
        DrawBar(renderer);
    }

    private void DrawBar(IRenderer renderer)
    {
        if (!TryGetImage(out var texture, out var region)) return;

        var scale = CurrentScale;
        var imageWidth = region.Width;
        var halfHeight = region.Height / 2;

        var baseY = region.Y;
        var fillY = region.Y + halfHeight;

        // Bar keeps its image size (scaled) and is centered inside the view bounds.
        var barWidth = imageWidth * scale;
        var barHeight = halfHeight * scale;
        var x = ScreenBounds.X + MathF.Floor((ScreenBounds.Width / scale - imageWidth) / 2) * scale;
        var y = ScreenBounds.Y + MathF.Floor((ScreenBounds.Height / scale - halfHeight) / 2) * scale;

        renderer.StateManager.SetTextureFilter(TextureFilter.Point);

        // Base – upper half, always visible.
        renderer.SpriteRenderer.Draw(texture,
            new RectangleF(x, y, barWidth, barHeight),
            new RectangleF(region.X, baseY, imageWidth, halfHeight));

        // Fill – lower half, cut from the center.
        var inset = Math.Min(AttachedView.FillInset ?? 0, imageWidth / 2);
        var fullFill = imageWidth - 2 * inset;
        var fill = FillWidth(fullFill);
        if (fill <= 0) return;

        var leftPart = (fill + 1) / 2;
        var rightPart = fill - leftPart;

        var offset = AttachedView.FillAlign switch
        {
            Align.Start => 0,
            Align.Center => (fullFill - fill) / 2,
            _ => fullFill - fill
        };

        var targetX = x + (inset + offset) * scale;
        var fillStart = region.X + inset;

        // Left piece: beginning of the fill.
        renderer.SpriteRenderer.Draw(texture,
            new RectangleF(targetX, y, leftPart * scale, barHeight),
            new RectangleF(fillStart, fillY, leftPart, halfHeight));

        // Right piece: end of the fill.
        if (rightPart > 0)
        {
            renderer.SpriteRenderer.Draw(texture,
                new RectangleF(targetX + leftPart * scale, y, rightPart * scale, barHeight),
                new RectangleF(fillStart + fullFill - rightPart, fillY, rightPart, halfHeight));
        }
    }

    /// <summary>Visible fill width in whole texture pixels.</summary>
    private int FillWidth(int fullFill)
    {
        if (fullFill <= 0) return 0;

        var max = AttachedView.MaxValue?.Value ?? 0;
        if (max <= 0) return 0;

        var value = Math.Clamp(AttachedView.Value?.Value ?? 0, 0, max);
        if (value == 0) return 0;

        // At least 1 pixel while the boss is alive; full width only at full HP.
        var width = (int)MathF.Round(fullFill * (float)value / max);
        return value == max ? fullFill : Math.Clamp(width, 1, Math.Max(1, fullFill - 1));
    }

    protected override void OnDispose(bool disposing)
    {
        base.OnDispose(disposing);

        if (!disposing) return;

        if (AttachedView.Source != null)
        {
            AttachedView.Source.ImageChanged -= OnImageChanged;
            AttachedView.Source.Dispose();
        }
    }
}
