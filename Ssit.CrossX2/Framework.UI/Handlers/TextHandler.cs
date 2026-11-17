using System.Numerics;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Font;
using Ssit.CrossX2.Framework.UI.Parameters;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI.Handlers;

/// <summary>
/// Handler of <see cref="Views.Text"/>: lays the text out wrapped to the text rectangle width (in font units) and draws
/// it line by line.
/// </summary>
public class TextHandler(ViewHandler.CreateHandlerParameters parameters, IFontsManager fontsManager)
    : TextBaseHandler<Views.Text>(parameters, fontsManager)
{
    // Wrap width (font units) of the current layout; -1 = not laid out yet.
    private int _layoutWidth = -1;

    // Height the last page layout request asked for - not asked again if the parent can't give it (no layout loop).
    private float _requestedHeight = -1;

    protected override void OnTextChanged()
    {
        CalculateLayout();

        // New text - a new height may be requested again.
        _requestedHeight = -1;

        CalculateSizeInternal(out var width, out var height);
        CalculateAlign(out var ha, out var va);

        if (width.IsAuto && ha != Align.Fill || height.IsAuto && va != Align.Fill)
        {
            Parent?.RecalculateLayout(AttachedView);
        }
    }

    public override void SetBounds(RectangleF rectangleF)
    {
        base.SetBounds(rectangleF);
        UpdateLayout();
    }

    /// <summary>
    /// Re-wraps the text when the width changed, and when an auto height doesn't match the wrapped text (the parent
    /// measured us before the width was known, or before the text arrived - e.g. while the parent was hidden), asks
    /// the page for a new layout. A page request only marks the layout dirty, so it's safe inside a layout pass
    /// (a parent's own pending requests are cleared when its pass ends).
    /// </summary>
    private void UpdateLayout()
    {
        if (WrapWidth != _layoutWidth)
        {
            CalculateLayout();
        }

        if (AttachedView.Height is { IsAuto: false })
            return;

        CalculateSize(out _, out var height);
        var textHeight = height.Calculate(CurrentScale, 0);

        if (MathF.Abs(textHeight - Bounds.Height) > 0.5f && MathF.Abs(textHeight - _requestedHeight) > 0.5f)
        {
            _requestedHeight = textHeight;
            Parent?.GetParent<IPage>()?.RecalculateLayout();
        }
    }

    /// <summary>
    /// Width of the text rectangle in font units (the glyph metrics are unscaled). 0 while the bounds aren't known.
    /// </summary>
    private int WrapWidth => Math.Max(0, (int)(TextRectangle.Width / TextScale));

    private void CalculateLayout()
    {
        var font = GetFont();
        var spacing = AttachedView.TextSpacing ?? TextSpacing.Normal;
        var lineSpacing = AttachedView.LineSpacing ?? 0;
        var wrapWidth = WrapWidth;

        TextRenderingContext.Reset();

        if (wrapWidth > 0)
        {
            // Same default as the text renderer.
            var paragraphSpacing = AttachedView.ParagraphSpacing;
            if (paragraphSpacing < 0)
            {
                paragraphSpacing = font is IGlyphFont glyphFont ? glyphFont.Metrics.LineHeight / 4f : 0;
            }

            font.CalculateMultilineText(AttachedView.Text, spacing, wrapWidth, paragraphSpacing, lineSpacing, TextRenderingContext);
        }
        else
        {
            // Bounds not known yet - unwrapped, so an auto size has a starting point.
            font.CalculateText(AttachedView.Text, spacing, lineSpacing, TextRenderingContext);
        }

        _layoutWidth = wrapWidth;

        // Interline: extra space above every line but the first. Before anything reads the (lazily computed) height.
        if (font is IGlyphFont interlineFont && MathF.Abs(AttachedView.Interline - 1) > float.Epsilon)
        {
            var extra = (AttachedView.Interline - 1) * interlineFont.Metrics.LineHeight;
            var lines = TextRenderingContext.Lines;

            for (var idx = 1; idx < lines.Count; idx++)
            {
                var line = lines[idx];
                line.Spacing += extra;
                lines[idx] = line;
            }
        }
    }

    protected override void OnDraw(IRenderer renderer)
    {
        // Drawn for the first time after being hidden - the bounds may have changed without a layout pass reaching us.
        UpdateLayout();

        base.OnDraw(renderer);

        if (GetFont() is not IGlyphFont font)
            return;

        var color = TextColor(renderer) ?? RgbaColor.Transparent;
        var outlineColor = TextOutlineColor(renderer) ?? RgbaColor.Transparent;

        var spacing = AttachedView.TextSpacing ?? TextSpacing.Normal;
        var align = AttachedView.TextAlign ?? ContentAlign.Left | ContentAlign.Top;
        var horizontalAlign = align & (ContentAlign.Center | ContentAlign.Right);

        var scale = TextScale;
        var rect = TextRectangle;
        var totalHeight = TextRenderingContext.Height * scale;

        var y = rect.Y;

        if ((align & ContentAlign.VCenter) == ContentAlign.VCenter)
        {
            y = rect.Center.Y - totalHeight / 2;
        }
        else if ((align & ContentAlign.Bottom) == ContentAlign.Bottom)
        {
            y = rect.Bottom - totalHeight;
        }

        var x = rect.X;

        if ((horizontalAlign & ContentAlign.Center) == ContentAlign.Center)
        {
            x = rect.Center.X;
        }
        else if ((horizontalAlign & ContentAlign.Right) == ContentAlign.Right)
        {
            x = rect.Right;
        }

        // Each line as a single-line text, top aligned - the same placement as the multiline text renderer.
        foreach (var line in TextRenderingContext.Lines)
        {
            y += line.Spacing * scale;

            renderer.TextRenderer.DrawText(
                font: font,
                text: line.Text,
                position: new Vector2(x, y),
                align: horizontalAlign | ContentAlign.Top,
                scale: scale,
                color: color,
                spacing: spacing,
                outlineColor: outlineColor,
                lineSpacing: 0);

            y += font.Metrics.LineHeight * scale;
        }
    }
}
