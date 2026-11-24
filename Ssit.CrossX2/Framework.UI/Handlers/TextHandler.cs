using System.Numerics;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Font;
using Ssit.CrossX2.Framework.Graphics.Internal;
using Ssit.CrossX2.Framework.Text;
using Ssit.CrossX2.Framework.UI.Parameters;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI.Handlers;

/// <summary>
/// Handler of <see cref="Views.Text"/>: lays the text out wrapped to the text rectangle width (in font units) and draws
/// it line by line.
/// </summary>
public class TextHandler(ViewHandler.CreateHandlerParameters parameters, IFontsManager fontsManager)
    : TextHandler<Views.Text>(parameters, fontsManager);

/// <summary>
/// Base of the handlers of <see cref="Views.Text"/> and the views derived from it: lays the text out wrapped to the text
/// rectangle width (in font units) once when the text (or the width) changes, and draws it line by line
/// (<see cref="DrawText"/>).
/// </summary>
public class TextHandler<TText>(ViewHandler.CreateHandlerParameters parameters, IFontsManager fontsManager)
    : TextBaseHandler<TText>(parameters, fontsManager) where TText : Views.Text
{
    // Wrap width (font units) of the current layout; -1 = not laid out yet.
    private int _layoutWidth = -1;

    // Height the last page layout request asked for - not asked again if the parent can't give it (no layout loop).
    private float _requestedHeight = -1;

    /// <summary>
    /// Word of a justified line: its text and its x offset from the line start (font units).
    /// </summary>
    protected readonly record struct JustifiedWord(TextSource Text, float X);

    // Words of the justified lines, and per laid-out line the range of its words (Count 0 = drawn as a whole line).
    private readonly List<JustifiedWord> _justifiedWords = [];
    private readonly List<(int First, int Count)> _justifiedLines = [];

    /// <summary>
    /// Words of the justified lines (see <see cref="JustifiedLines"/>).
    /// </summary>
    protected IReadOnlyList<JustifiedWord> JustifiedWords => _justifiedWords;

    /// <summary>
    /// Per laid-out line the range of its words in <see cref="JustifiedWords"/>; Count 0 = the line is drawn as a whole.
    /// </summary>
    protected IReadOnlyList<(int First, int Count)> JustifiedLines => _justifiedLines;

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

        CalculateJustification(font, spacing, wrapWidth);
    }

    /// <summary>
    /// Whether the wrapped lines are justified: <see cref="ContentAlign.Justified"/> in <see cref="Label.TextAlign"/>.
    /// </summary>
    private bool IsJustified
        => ((AttachedView.TextAlign ?? ContentAlign.Left) & ContentAlign.Justified) == ContentAlign.Justified;

    /// <summary>
    /// With <see cref="IsJustified"/>, splits every wrapped line (not the last line of a paragraph, nor a line
    /// without spaces) into words and places them so the stretched spaces fill <paramref name="wrapWidth"/>.
    /// No-break spaces are stretched too (the line was only never broken at them).
    /// </summary>
    private void CalculateJustification(IFont font, TextSpacing spacing, int wrapWidth)
    {
        _justifiedWords.Clear();
        _justifiedLines.Clear();

        if (!IsJustified || wrapWidth <= 0 || font is not IGlyphFont glyphFont)
            return;

        foreach (var line in TextRenderingContext.Lines)
        {
            if (line.EndOfParagraph || line.Whitespaces == 0)
            {
                _justifiedLines.Add((0, 0));
                continue;
            }

            // Extra width of every breaking space - never negative (a line wider than the wrap width just keeps its
            // spaces). The spaces keep their own width (a space glyph's advance, or the font's whitespace width).
            var extraSpaceWidth = MathF.Max(0, (wrapWidth - line.Width) / line.Whitespaces);

            var first = _justifiedWords.Count;
            var text = line.Text;
            var x = 0f;
            var wordStart = 0;

            for (var idx = 0; idx <= text.Length; idx++)
            {
                if (idx < text.Length && !GlyphFontRenderer.IsStretchableSpace(glyphFont, text[idx]))
                    continue;

                if (idx > wordStart)
                {
                    var word = new TextSource(text, wordStart, idx - wordStart);
                    _justifiedWords.Add(new JustifiedWord(word, x));
                    x += GlyphFontRenderer.MeasureWidth(glyphFont, word, spacing);
                }

                if (idx < text.Length)
                {
                    x += GlyphFontRenderer.MeasureWidth(glyphFont, new TextSource(text, idx, 1), spacing) + extraSpaceWidth;
                }

                wordStart = idx + 1;
            }

            _justifiedLines.Add((first, _justifiedWords.Count - first));
        }
    }

    protected override void OnDraw(IRenderer renderer)
    {
        // Drawn for the first time after being hidden - the bounds may have changed without a layout pass reaching us.
        UpdateLayout();

        base.OnDraw(renderer);

        if (GetFont() is not IGlyphFont font)
            return;

        DrawText(renderer, font);
    }

    /// <summary>
    /// Position of the laid-out text: y of its top, and x of the line anchor - the left edge, the center or the right edge
    /// of the text rectangle by the horizontal align of <paramref name="align"/>.
    /// </summary>
    protected void CalculateTextOrigin(ContentAlign align, out float x, out float y)
    {
        var horizontalAlign = align & (ContentAlign.Center | ContentAlign.Right);

        var rect = TextRectangle;
        var totalHeight = TextRenderingContext.Height * TextScale;

        y = rect.Y;

        if ((align & ContentAlign.VCenter) == ContentAlign.VCenter)
        {
            y = rect.Center.Y - totalHeight / 2;
        }
        else if ((align & ContentAlign.Bottom) == ContentAlign.Bottom)
        {
            y = rect.Bottom - totalHeight;
        }

        x = rect.X;

        if ((horizontalAlign & ContentAlign.Center) == ContentAlign.Center)
        {
            x = rect.Center.X;
        }
        else if ((horizontalAlign & ContentAlign.Right) == ContentAlign.Right)
        {
            x = rect.Right;
        }
    }

    /// <summary>
    /// Draws the laid-out lines (<see cref="TextBaseHandler{TTextView}.TextRenderingContext"/>, <see cref="JustifiedLines"/>).
    /// </summary>
    protected virtual void DrawText(IRenderer renderer, IGlyphFont font)
    {
        var color = TextColor(renderer) ?? RgbaColor.Transparent;
        var outlineColor = TextOutlineColor(renderer) ?? RgbaColor.Transparent;

        var spacing = AttachedView.TextSpacing ?? TextSpacing.Normal;
        var align = AttachedView.TextAlign ?? ContentAlign.Left | ContentAlign.Top;
        var horizontalAlign = align & (ContentAlign.Center | ContentAlign.Right);

        var scale = TextScale;
        var rect = TextRectangle;

        CalculateTextOrigin(align, out var x, out var y);

        var lines = TextRenderingContext.Lines;

        // Each line as a single-line text, top aligned - the same placement as the multiline text renderer.
        for (var lineIdx = 0; lineIdx < lines.Count; lineIdx++)
        {
            var line = lines[lineIdx];
            y += line.Spacing * scale;

            var (firstWord, wordCount) = lineIdx < _justifiedLines.Count ? _justifiedLines[lineIdx] : (0, 0);

            if (wordCount > 0)
            {
                // Justified line - fills the whole width, word by word from the left edge.
                for (var wordIdx = firstWord; wordIdx < firstWord + wordCount; wordIdx++)
                {
                    var word = _justifiedWords[wordIdx];

                    renderer.TextRenderer.DrawText(
                        font: font,
                        text: word.Text,
                        // Whole pixels - the stretched spaces would put the glyphs between pixels.
                        position: new Vector2(MathF.Round(rect.X + word.X * scale), y),
                        align: ContentAlign.Left | ContentAlign.Top,
                        scale: scale,
                        color: color,
                        spacing: spacing,
                        outlineColor: outlineColor,
                        lineSpacing: 0);
                }
            }
            else
            {
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
            }

            y += font.Metrics.LineHeight * scale;
        }
    }
}
