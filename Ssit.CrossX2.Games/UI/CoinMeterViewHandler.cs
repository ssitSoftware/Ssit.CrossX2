using System.Globalization;
using System.Numerics;
using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Font;
using Ssit.CrossX2.Framework.UI.Handlers;
using Ssit.CrossX2.Framework.UI.Parameters;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.Games.UI;

internal class CoinMeterViewHandler: BackgroundHandler<CoinMeterView>
{
    private readonly IContentManager _contentManager;
    private ResourceHandle<ITexture> _texture;
    private string _texturePath;
    private readonly IFontsManager _fontsManager;
    private readonly IColorSource _colorSource;

    private string _text = "0";

    public CoinMeterViewHandler(CreateHandlerParameters parameters, IContentManager contentManager, IFontsManager fontsManager) : base(parameters)
    {
        if (string.IsNullOrEmpty(AttachedView.IconTexturePath?.ToString()))
        {
            throw new InvalidOperationException("CoinMeterView is missing IconTexturePath.");
        }

        if (string.IsNullOrEmpty(AttachedView.Font?.FontFamily))
        {
            throw new InvalidOperationException("CoinMeterView is missing Font.");
        }

        _contentManager = contentManager;
        _fontsManager = fontsManager;
        _colorSource = parameters.Parent?.GetParent<IColorSource>(true);

        LoadTexture();
        AttachedView.IconTexturePath.TextChanged += OnIconTexturePathChanged;

        UpdateText(AttachedView.Value?.Value ?? 0);

        if (AttachedView.Value != null)
        {
            AttachedView.Value.ValueChanged += OnValueChanged;
        }
    }

    private void LoadTexture()
    {
        var path = AttachedView.IconTexturePath?.ToString();

        // Keep the current icon when the path is cleared or did not actually change.
        if (string.IsNullOrEmpty(path) || path == _texturePath) return;

        var oldTexture = _texture;
        _texture = _contentManager.Get<ITexture>(path);
        _texturePath = path;
        oldTexture?.Dispose();
    }

    private void OnIconTexturePathChanged()
    {
        var oldSize = _texture?.Resource.Size;
        LoadTexture();

        // Different icon size changes the layout of the view.
        if (oldSize != IconSize)
        {
            Parent?.RecalculateLayout(AttachedView);
        }
    }

    private float Opacity => Math.Clamp(AttachedView.Opacity?.Value ?? 1, 0, 1);

    private RgbaColor? TextColor(IRenderer renderer) => AttachedView.TextColor?.GetColor(renderer, _colorSource);
    private RgbaColor? TextOutlineColor(IRenderer renderer) => AttachedView.TextOutlineColor?.GetColor(renderer, _colorSource);

    private Size IconSize => _texture.Resource.Size;

    // Pixel font - it is scaled by CurrentScale when drawn, like Label with TextScaling.Pixel.
    // FontSize 0 picks the font's native size.
    private IFont Font => _fontsManager.GetFont(
        AttachedView.Font.Value.FontFamily,
        AttachedView.Font.Value.FontSize);

    private Size TextSize => Font.TextSize(_text);

    // The text is drawn with no extra line spacing, so the line box starts exactly at the draw position.
    private const int TextLineSpacing = 0;

    /// <summary>
    /// Vertical ink bounds of the digits ("0" glyph), in font pixels: Top is relative to the text draw position,
    /// Height is the glyph's real height. Used instead of the line height so the digits - not the whole line box
    /// (ascender, descender, line gap) - are centered to the icon.
    /// </summary>
    private static (float Top, int Height) DigitBounds(IFont font)
    {
        if (font is IGlyphFont glyphFont && glyphFont.GetGlyph('0') is { Source.Height: > 0 } glyph)
        {
            // Same placement as GlyphFontRenderer for top-left aligned text:
            // y = position - Ascender + lineSpacing + glyph.Offset.Y.
            return (glyph.Offset.Y - glyphFont.Metrics.Ascender + TextLineSpacing, glyph.Source.Height);
        }

        // No glyph metrics - fall back to the line box.
        return (0, font.TextSize("0").Height);
    }

    private void UpdateText(int value)
    {
        // "D<n>" pads with leading zeros to at least n digits (sign is kept in front).
        var minDigits = Math.Max(1, AttachedView.MinDigits);
        _text = value.ToString("D" + minDigits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }

    private void OnValueChanged(int value)
    {
        var oldLength = _text.Length;
        UpdateText(value);

        // Monospace pixel font: the size changes only when the number of digits changes.
        if (oldLength != _text.Length)
        {
            Parent?.RecalculateLayout(AttachedView);
        }
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

        var iconSize = IconSize;
        var textSize = TextSize;
        var digitHeight = DigitBounds(Font).Height;

        if (width.IsAuto)
        {
            width = horizontalAlign == Align.Fill
                ? Length.Fill
                : (Length)(iconSize.Width + textSize.Width) + AttachedView.Spacing;
        }

        if (height.IsAuto)
        {
            height = verticalAlign == Align.Fill
                ? Length.Fill
                : (Length)Math.Max(iconSize.Height, digitHeight);
        }
    }

    protected override void OnDraw(IRenderer renderer)
    {
        // UI must never be affected by scene lighting.
        renderer.LightingManager.EnableLighting(false);

        base.OnDraw(renderer);

        var opacity = Opacity;
        if (opacity <= 0) return;

        var scale = CurrentScale;
        var font = Font;
        var iconSize = IconSize;
        var textSize = font.TextSize(_text);
        var (digitTop, digitHeight) = DigitBounds(font);

        var iconWidth = iconSize.Width * scale;
        var iconHeight = iconSize.Height * scale;
        var textWidth = textSize.Width * scale;
        var spacing = AttachedView.Spacing.Calculate(scale, ScreenBounds.Width);

        var contentHeight = Math.Max(iconSize.Height, digitHeight) * scale;
        var top = ScreenBounds.Y + (ScreenBounds.Height - contentHeight) / 2;

        // Icon and digits centered to each other, snapped to whole design pixels.
        // The digits' ink ("0" glyph) is centered, then shifted back by its offset inside the line box.
        var iconY = top + MathF.Floor((contentHeight / scale - iconSize.Height) / 2) * scale;
        var digitY = top + MathF.Floor((contentHeight / scale - digitHeight) / 2) * scale;
        var textY = digitY - digitTop * scale;

        float iconX, textX;
        if (AttachedView.TextOnRight)
        {
            iconX = ScreenBounds.X;
            textX = iconX + iconWidth + spacing;
        }
        else
        {
            textX = ScreenBounds.X;
            iconX = textX + textWidth + spacing;
        }

        renderer.StateManager.SetTextureFilter(TextureFilter.Point);

        renderer.SpriteRenderer.Draw(_texture.Resource,
            new RectangleF(iconX, iconY, iconWidth, iconHeight),
            new RectangleF(0, 0, iconSize.Width, iconSize.Height),
            nullableColor: RgbaColor.White * opacity);

        renderer.TextRenderer.DrawText(
            font: font,
            text: _text,
            position: new Vector2(textX, textY),
            align: ContentAlign.Left,
            scale: scale,
            lineSpacing: TextLineSpacing,
            color: (TextColor(renderer) ?? RgbaColor.Transparent) * opacity,
            outlineColor: (TextOutlineColor(renderer) ?? RgbaColor.Transparent) * opacity);
    }

    protected override void OnDispose(bool disposing)
    {
        base.OnDispose(disposing);

        if (!disposing) return;

        if (AttachedView.Value != null)
        {
            AttachedView.Value.ValueChanged -= OnValueChanged;
        }

        if (AttachedView.IconTexturePath != null)
        {
            AttachedView.IconTexturePath.TextChanged -= OnIconTexturePathChanged;
        }

        _texture?.Dispose();
        _texture = null;
    }
}
