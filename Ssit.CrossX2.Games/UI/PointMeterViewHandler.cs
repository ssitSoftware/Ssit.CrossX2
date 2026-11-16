using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Handlers;
using Ssit.CrossX2.Framework.UI.Parameters;

namespace Ssit.CrossX2.Framework.Games.UI;

internal class PointMeterViewHandler: BackgroundHandler<PointMeterView>
{
    private readonly ResourceHandle<ITexture> _texture;

    public PointMeterViewHandler(CreateHandlerParameters parameters, IContentManager contentManager) : base(parameters)
    {
        if (string.IsNullOrEmpty(AttachedView.PointsTexturePath))
        {
            throw new InvalidOperationException("PointMeterView is missing HpTexturePath.");
        }

        if (AttachedView.FillLevels < 2)
        {
            throw new InvalidOperationException("PointMeterView.FillLevels must be at least 2 (empty and full frame).");
        }

        if (AttachedView.FillValue < 1)
        {
            throw new InvalidOperationException("PointMeterView.FillValue must be at least 1.");
        }

        _texture = contentManager.Get<ITexture>(AttachedView.PointsTexturePath);

        if (AttachedView.MaxValue != null)
        {
            AttachedView.MaxValue.ValueChanged += OnMaxValueChanged;
        }
    }

    /// <summary>Width of one frame in texture pixels.</summary>
    private int ColumnWidth => AttachedView.TextureColumnWidth > 0
        ? AttachedView.TextureColumnWidth
        : _texture.Resource.Size.Width / AttachedView.FillLevels;

    private int FrameHeight => _texture.Resource.Size.Height;

    /// <summary>Levels in one heart (frame 0 is empty, so it does not count).</summary>
    private int LevelsPerHeart => AttachedView.FillLevels - 1;

    /// <summary>HP held by one full heart.</summary>
    private int HeartValue => AttachedView.FillValue * LevelsPerHeart;

    private int HeartsCount
    {
        get
        {
            var max = AttachedView.MaxValue?.Value ?? 0;
            if (max <= 0) return 0;
            return (max + HeartValue - 1) / HeartValue;
        }
    }

    private void OnMaxValueChanged(int _)
    {
        Parent?.RecalculateLayout(AttachedView);
    }

    public override void CalculateAlign(out Align horizontalAlign, out Align verticalAlign)
    {
        horizontalAlign = AttachedView.HorizontalAlign ?? Align.Start;
        verticalAlign = AttachedView.VerticalAlign ?? Align.Start;
    }

    public override void CalculateSize(out Length width, out Length height)
    {
        CalculateAlign(out var horizontalAlign, out var verticalAlign);

        width = AttachedView.Width ?? Length.Auto;
        height = AttachedView.Height ?? Length.Auto;

        if (width.IsAuto)
        {
            if (horizontalAlign == Align.Fill)
            {
                width = Length.Fill;
            }
            else
            {
                var count = HeartsCount;
                if (count > 0)
                {
                    Length heartsWidth = count * ColumnWidth;
                    width = heartsWidth + AttachedView.Spacing * (count - 1);
                }
                else
                {
                    width = Length.Zero;
                }
            }
        }

        if (height.IsAuto)
        {
            height = verticalAlign == Align.Fill ? Length.Fill : (Length)FrameHeight;
        }
    }

    protected override void OnDraw(IRenderer renderer)
    {
        // HP is UI – it must never be affected by scene lighting.
        var lighting = renderer.LightingManager;
        lighting.EnableLighting(false);
        
        base.OnDraw(renderer);
        DrawHearts(renderer);
    }

    private void DrawHearts(IRenderer renderer)
    {
        var count = HeartsCount;
        if (count == 0) return;

        var scale = CurrentScale;
        var texture = _texture.Resource;
        var columnWidth = ColumnWidth;
        var frameHeight = FrameHeight;

        var heartWidth = columnWidth * scale;
        var heartHeight = frameHeight * scale;
        var spacing = AttachedView.Spacing.Calculate(scale, ScreenBounds.Width);

        var value = Math.Max(0, AttachedView.Value?.Value ?? 0);
        var heartValue = HeartValue;
        var fillValue = AttachedView.FillValue;
        var levelsPerHeart = LevelsPerHeart;

        var x = ScreenBounds.X;
        var y = ScreenBounds.Y + (ScreenBounds.Height - heartHeight) / 2;

        renderer.StateManager.SetTextureFilter(TextureFilter.Point);

        for (var idx = 0; idx < count; ++idx)
        {
            var hpInHeart = Math.Clamp(value - idx * heartValue, 0, heartValue);

            // Ceiling: a partially filled level shows as the next higher level,
            // so a heart looks empty only when its HP is actually 0.
            var level = Math.Clamp((hpInHeart + fillValue - 1) / fillValue, 0, levelsPerHeart);

            var source = new RectangleF(level * columnWidth, 0, columnWidth, frameHeight);
            var target = new RectangleF(x, y, heartWidth, heartHeight);

            renderer.SpriteRenderer.Draw(texture, target, source);

            x += heartWidth + spacing;
        }
    }

    protected override void OnDispose(bool disposing)
    {
        base.OnDispose(disposing);

        if (!disposing) return;

        if (AttachedView.MaxValue != null)
        {
            AttachedView.MaxValue.ValueChanged -= OnMaxValueChanged;
        }

        _texture?.Dispose();
    }
}
