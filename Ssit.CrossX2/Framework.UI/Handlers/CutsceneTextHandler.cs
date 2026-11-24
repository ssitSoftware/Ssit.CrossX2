using System.Numerics;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Font;
using Ssit.CrossX2.Framework.Graphics.Internal;
using Ssit.CrossX2.Framework.Text;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI.Handlers;

/// <summary>
/// Handler of <see cref="CutsceneText"/>: the layout of the whole text (made by <see cref="TextHandler{TText}"/> only
/// when the text or the width changes) is drawn up to <see cref="CutsceneText.VisibleCharacters"/>, every character in
/// its final place, followed by the blinking cursor.
/// </summary>
public class CutsceneTextHandler : TextHandler<CutsceneText>
{
    // Time since the visible characters last changed - the cursor blinks from it.
    private float _blinkTime;

    // The cursor character as a text (cached).
    private string _cursorText;

    public CutsceneTextHandler(CreateHandlerParameters parameters, IFontsManager fontsManager) : base(parameters, fontsManager)
    {
        if (AttachedView.VisibleCharacters is not null)
        {
            AttachedView.VisibleCharacters.ValueChanged += OnVisibleCharactersChanged;
        }
    }

    private int VisibleCharacters => AttachedView.VisibleCharacters?.Value ?? int.MaxValue;

    private bool IsCursorShown
    {
        get
        {
            // No text at all - no cursor. An empty text still shows it (blinking, nothing typed yet).
            if (AttachedView.CursorCharacter == '\0' || AttachedView.Text is null)
                return false;

            var blinkTime = AttachedView.CursorBlinkTime;
            return blinkTime <= 0 || _blinkTime % (2 * blinkTime) < blinkTime;
        }
    }

    private void OnVisibleCharactersChanged(int _)
    {
        // Solid cursor while typing.
        _blinkTime = 0;
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        _blinkTime += dt;
    }

    protected override void DrawText(IRenderer renderer, IGlyphFont font)
    {
        var color = TextColor(renderer) ?? RgbaColor.Transparent;
        var outlineColor = TextOutlineColor(renderer) ?? RgbaColor.Transparent;

        var spacing = AttachedView.TextSpacing ?? TextSpacing.Normal;
        var align = AttachedView.TextAlign ?? ContentAlign.Left | ContentAlign.Top;
        var horizontalAlign = align & (ContentAlign.Center | ContentAlign.Right);

        var scale = TextScale;
        var rect = TextRectangle;
        var visible = VisibleCharacters;

        CalculateTextOrigin(align, out var x, out var y);

        var lines = TextRenderingContext.Lines;

        // Right after the last visible character - in the last line (word) that has started.
        Vector2? cursorPosition = null;

        for (var lineIdx = 0; lineIdx < lines.Count; lineIdx++)
        {
            var line = lines[lineIdx];
            var lineStart = line.Text.Start;

            // No visible character from here on (the first line still gets the cursor at its start).
            if (lineStart > visible && lineIdx > 0)
                break;

            y += line.Spacing * scale;

            var (firstWord, wordCount) = lineIdx < JustifiedLines.Count ? JustifiedLines[lineIdx] : (0, 0);
            float cursorX;

            if (wordCount > 0)
            {
                // Justified line - every word in its place in the stretched line.
                cursorX = MathF.Round(rect.X);

                for (var wordIdx = firstWord; wordIdx < firstWord + wordCount; wordIdx++)
                {
                    var word = JustifiedWords[wordIdx];
                    var wordStart = word.Text.Start;

                    if (wordStart > visible)
                        break;

                    var wordX = MathF.Round(rect.X + word.X * scale);
                    cursorX = DrawVisiblePart(renderer, font, word.Text, visible - wordStart, wordX, y, scale, color, outlineColor, spacing);
                }
            }
            else
            {
                // Placed by the width of the whole line, so its visible part doesn't move while typing.
                var lineX = x;

                if ((horizontalAlign & ContentAlign.Center) == ContentAlign.Center)
                {
                    lineX -= line.Width * scale / 2;
                }
                else if ((horizontalAlign & ContentAlign.Right) == ContentAlign.Right)
                {
                    lineX -= line.Width * scale;
                }

                cursorX = DrawVisiblePart(renderer, font, line.Text, visible - lineStart, MathF.Round(lineX), y, scale, color, outlineColor, spacing);
            }

            cursorPosition = new Vector2(cursorX, y);
            y += font.Metrics.LineHeight * scale;
        }

        // No laid-out line (an empty text) - the cursor at the start of the text.
        if (cursorPosition is null && lines.Count == 0)
        {
            cursorPosition = new Vector2(MathF.Round(x), y);
        }

        if (cursorPosition is { } position && IsCursorShown)
        {
            if (_cursorText is null || _cursorText[0] != AttachedView.CursorCharacter)
            {
                _cursorText = AttachedView.CursorCharacter.ToString();
            }

            DrawPart(renderer, font, _cursorText, position.X, position.Y, scale, color, outlineColor, spacing);
        }
    }

    /// <summary>
    /// Draws the first <paramref name="visible"/> characters of the text at the position (left, top) and returns x right
    /// after them.
    /// </summary>
    private static float DrawVisiblePart(IRenderer renderer, IGlyphFont font, TextSource text, int visible, float x, float y,
        float scale, RgbaColor color, RgbaColor outlineColor, TextSpacing spacing)
    {
        var length = Math.Clamp(visible, 0, text.Length);

        if (length == 0)
            return x;

        var part = new TextSource(text, 0, length);
        DrawPart(renderer, font, part, x, y, scale, color, outlineColor, spacing);

        return x + GlyphFontRenderer.MeasureWidth(font, part, spacing) * scale;
    }

    private static void DrawPart(IRenderer renderer, IGlyphFont font, TextSource text, float x, float y, float scale,
        RgbaColor color, RgbaColor outlineColor, TextSpacing spacing)
    {
        renderer.TextRenderer.DrawText(
            font: font,
            text: text,
            position: new Vector2(x, y),
            align: ContentAlign.Left | ContentAlign.Top,
            scale: scale,
            color: color,
            spacing: spacing,
            outlineColor: outlineColor,
            lineSpacing: 0);
    }

    protected override void OnDispose(bool disposing)
    {
        base.OnDispose(disposing);

        if (AttachedView.VisibleCharacters is not null)
        {
            AttachedView.VisibleCharacters.ValueChanged -= OnVisibleCharactersChanged;
        }
    }
}
