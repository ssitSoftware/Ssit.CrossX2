using System.Numerics;
using Ssit.CrossX2.Framework.Graphics.Font;
using Ssit.CrossX2.Framework.Graphics.Renderers;
using Ssit.CrossX2.Framework.Text;

namespace Ssit.CrossX2.Framework.Graphics.Internal;

public delegate void DrawTextureQuadDelegate(ITexture texture,
    RectangleF target, Rectangle? source, RgbaColor? color);

internal static class GlyphFontRenderer
{
    private static readonly TextRenderingContext TempContext = new();

    /// <summary>
    /// No-break space (&amp;#160; in XML): drawn like a space, but a line never breaks at it and justification doesn't
    /// stretch it.
    /// </summary>
    public const char NonBreakingSpace = '\u00A0';

    /// <summary>
    /// True for a character a line can break at and justification stretches: a space, or any character without a glyph
    /// except the no-break space.
    /// </summary>
    internal static bool IsBreakingSpace(IGlyphFont font, char c) =>
        c == ' ' || c != NonBreakingSpace && font.GetGlyph(c) == null;
    
    public static void RenderText(IQuadsRenderer quadsRenderer, IGlyphFont font, TextSource text, Vector2 position, ContentAlign align, float scale,
        RgbaColor color, RgbaColor outlineColor, TextSpacing spacing, int lineSpacing, TextRenderingContext context)
    {
        if (context is null)
        {
            context = TempContext;
            CalculateText(font, text, spacing, lineSpacing, context);
        }
        else if(!context.IsValid(text, font, spacing))
        {
            CalculateText(font, text, spacing, lineSpacing, context);
        }
        
        if (font.OutlineSheet is not null && outlineColor.A > 0)
        {
            RenderText(quadsRenderer.Draw, font.OutlineSheet, font, context.Lines, position, align, scale,
                outlineColor, spacing, context.Width, context.Height);
        }

        if (color.A > 0)
        {
            RenderText(quadsRenderer.Draw, font.FontSheet, font, context.Lines, position, align, scale, 
                color, spacing, context.Width, context.Height);
        }
    }

    public static void RenderText(IQuadsRenderer quadsRenderer, IGlyphFont font, TextSource text, RectangleF target, ContentAlign align, float scale,
        RgbaColor color, RgbaColor outlineColor, TextSpacing spacing, float paragraphSpacing, int lineSpacing, TextRenderingContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context), "TextRenderingContext must be not null!");
        }
        
        if (!context.IsValid(text, font, spacing, (int)target.Width))
        {
            CalculateMultilineText(font, text, target.Width, spacing, paragraphSpacing, lineSpacing, context);
        }

        var position = target.TopLeft;

        if ((align & ContentAlign.Center) == ContentAlign.Center)
        {
            position.X = target.Center.X;
        }
        
        if ((align & ContentAlign.VCenter) == ContentAlign.VCenter)
        {
            position.Y = target.Center.Y;
        }
        
        if ((align & ContentAlign.Right) == ContentAlign.Right)
        {
            position.X = target.Right;
        }
        
        if ((align & ContentAlign.Bottom) == ContentAlign.Bottom)
        {
            position.Y = target.Bottom;
        }
        
        if (font.OutlineSheet is not null && outlineColor.A > 0)
        {
            RenderText(quadsRenderer.Draw, font.OutlineSheet, font, context.Lines, position, align, scale,
                outlineColor, spacing, context.Width, context.Height);
        }

        if (color.A > 0)
        {
            RenderText(quadsRenderer.Draw, font.FontSheet, font, context.Lines, position, align, scale,
                color, spacing, context.Width, context.Height);
        }
    }

    public static void CalculateText(IGlyphFont font, TextSource text, TextSpacing spacing, int lineSpacing, TextRenderingContext context)
    {
        if (!context.IsValid(text, font, spacing, lineSpacing: lineSpacing))
        {
            context.Update(text, font, spacing, targetWidth: lineSpacing);
            CalculateLines(font, text, spacing, lineSpacing, context);
        }
    }

    public static void CalculateMultilineText(IGlyphFont font, TextSource text, float targetWidth,
        TextSpacing spacing, float paragraphSpacing, int lineSpacing, TextRenderingContext context)
    {
        if (context.IsValid(text, font, spacing, (int)targetWidth))
        {
            return;
        }
        
        context.Update(text, font, spacing, targetWidth: (int)targetWidth, lineSpacing: lineSpacing);
        CalculateWrapLines(font, text, spacing, paragraphSpacing, lineSpacing, context, (int)targetWidth);
    }

    private static void CalculateWrapLines(IGlyphFont font, TextSource text, TextSpacing spacing, float paragraphSpacing, int lineSpacing, TextRenderingContext context, int maxWidth)
    {
        context.Lines.Clear();

        var start = 0;
        var lastSmaller = 0;

        paragraphSpacing = MathF.Max(paragraphSpacing, lineSpacing);

        var newLineSpacing = (float)lineSpacing;
        
        for (var idx = 0; idx < text.Length + 1; ++idx)
        {
            if (text[idx] == '\n' || text[idx] == '\0')
            {
                var width = MeasureWidth(font, new TextSource(text, start, idx - start), spacing);

                if (width < maxWidth)
                {
                    var line = GetLine(text, start, idx - start, font, spacing);
                    line.Spacing = newLineSpacing;
                    line.EndOfParagraph = true;
                    newLineSpacing = paragraphSpacing;
                    start = idx + 1;
                    lastSmaller = start;
                    context.Lines.Add(line);
                }
                else if (lastSmaller > start)
                {
                    var line = GetLine(text, start, lastSmaller - start, font, spacing);
                    line.Spacing = newLineSpacing;
                    newLineSpacing = lineSpacing;
                    start = lastSmaller + 1;
                    lastSmaller = start;
                    idx = start;
                    context.Lines.Add(line);
                }
                else
                {
                    // Nowhere to break (one word wider than maxWidth) - keep it as an overflowing line rather than
                    // dropping the text.
                    var line = GetLine(text, start, idx - start, font, spacing);
                    line.Spacing = newLineSpacing;
                    line.EndOfParagraph = true;
                    newLineSpacing = paragraphSpacing;
                    start = idx + 1;
                    lastSmaller = start;
                    context.Lines.Add(line);
                }
                continue;
            }

            // A line can break at a space (fonts may have a glyph for it) or at any character without a glyph - but never
            // at a no-break space.
            if (IsBreakingSpace(font, text[idx]))
            {
                var width = MeasureWidth(font, new TextSource(text, start, idx - start), spacing);
                if (width < maxWidth)
                {
                    lastSmaller = idx;
                }
                else
                {
                    if (lastSmaller > start)
                    {
                        var line = GetLine(text, start, lastSmaller - start, font, spacing);
                        line.Spacing = newLineSpacing;
                        newLineSpacing = 0;
                        start = lastSmaller + 1;
                        idx = start;
                        context.Lines.Add(line);
                        if(MathF.Floor(line.Width) > maxWidth)
                        {
                            Console.WriteLine();
                        }
                    }
                    else
                    {
                        var line = GetLine(text, start, idx - start, font, spacing);
                        line.EndOfParagraph = true;
                        line.Spacing = newLineSpacing;
                        newLineSpacing = 0;
                        start = idx + 1;
                        lastSmaller = start;
                        context.Lines.Add(line);
                        if(MathF.Floor(line.Width) > maxWidth)
                        {
                            Console.WriteLine();
                        }
                    }
                }
            }
        }
    }

    internal static void RenderText(DrawTextureQuadDelegate drawDelegate, ITexture texture, IGlyphFont font, IReadOnlyList<TextRenderingContext.LineDefinition> lines, 
        Vector2 position, ContentAlign align, float scale, RgbaColor color, TextSpacing spacing, float justifyWidth, float height)
    {
        var additionalSpacing = (int)(spacing - 50) * font.Metrics.WhitespaceWidth / 50f;
        var posY = CalculateYPosition(font, position.Y, height, align, scale);

        for (var idx = 0; idx < lines.Count; ++idx)
        {
            var line = lines[idx];
            var posX = CalculateXPosition(position.X, align, line.Width * scale);
            posY += line.Spacing * scale;

            var whitespaceSize = (font.Metrics.WhitespaceWidth + additionalSpacing) * scale;
            // Not stretched by justification.
            var nonBreakingSpaceSize = whitespaceSize;

            if ((align & ContentAlign.Justified) == ContentAlign.Justified && line is { Whitespaces: > 0, EndOfParagraph: false } && line.Width * scale < justifyWidth + whitespaceSize)
            {
                var lineWidthNoSpaces = line.Width * scale - line.Whitespaces * whitespaceSize;
                whitespaceSize = (justifyWidth - lineWidthNoSpaces) / line.Whitespaces;
            }
            
            var previousCharacter = '\0';
            for(var i = 0; i < line.Text.Length; ++i)
            {
                var c = line.Text[i];
                var glyph = font.GetGlyph(c);
             
                if (glyph == null)
                {
                    posX += c == NonBreakingSpace ? nonBreakingSpaceSize : whitespaceSize;
                    previousCharacter = '\0';
                    continue;
                }

                posX += glyph.GetKerning(previousCharacter) * scale;
            
                var target = new RectangleF(posX + glyph.Offset.X * scale, posY + glyph.Offset.Y * scale, glyph.Source.Width * scale, glyph.Source.Height * scale);
                drawDelegate(texture, target, glyph.Source, color: color);

                posX += (glyph.Advance + additionalSpacing) * scale;
                previousCharacter = c;
            }

            posY += font.Metrics.LineHeight * scale;
        }
    }

    private static float CalculateXPosition(float positionX, ContentAlign align, float lineWidth)
    {
        if ((align & ContentAlign.Center) == ContentAlign.Center)
        {
            return MathF.Ceiling(positionX - lineWidth / 2f);
        }

        if ((align & ContentAlign.Right) == ContentAlign.Right)
        {
            return positionX - lineWidth;
        }

        return positionX;
    }

    private static float CalculateYPosition(IGlyphFont font, float positionY, float height, ContentAlign align, float scale)
    {
        if ((align & ContentAlign.VCenter) == ContentAlign.VCenter)
        {
            return MathF.Ceiling(positionY - (height/ 2f - font.Metrics.LineHeight / 2f) * scale + font.Metrics.XHeight / 2f * scale);
        }
        
        if ((align & ContentAlign.Bottom) == ContentAlign.Bottom)
        {
            return MathF.Ceiling(positionY - (height - font.Metrics.LineHeight) * scale );
        }

        return positionY - font.Metrics.Ascender * scale;
    }

    private static void CalculateLines(IGlyphFont font, TextSource text, TextSpacing spacing, int lineSpacing,
        TextRenderingContext context)
    {
        context.Lines.Clear();
        var length = text.Length;
        var position = 0;
        
        for (var idx = 0; idx < length; ++idx)
        {
            if (text[idx] != '\n') continue;
            
            var line = GetLine(text, position, idx - position, font, spacing);
            line.Spacing += lineSpacing;
            context.Lines.Add(line);
            position = idx + 1;
        }

        var lastLine = GetLine(text, position, length - position, font, spacing);
        lastLine.Spacing += lineSpacing;
        context.Lines.Add(lastLine);
    }

    private static TextRenderingContext.LineDefinition GetLine(TextSource source, int start, int length, IGlyphFont font, TextSpacing spacing)
    {
        var line = new TextSource(source, start, length);
        var width = MeasureWidth(font, line, spacing);
        var whitespaces = 0;
                
        for (var i = 0; i < line.Length; ++i)
        {
            // Only the stretchable ones - a no-break space keeps its width.
            if (font.GetGlyph(line[i]) is null && line[i] != NonBreakingSpace)
            {
                whitespaces++;
            }
        }

        return new TextRenderingContext.LineDefinition
        {
            Text = line,
            Width = width,
            Whitespaces = whitespaces,
            EndOfParagraph = false
        };
    }
    
    internal static float MeasureWidth(IGlyphFont glyphFont, TextSource text, TextSpacing spacing)
    {
        float width = 0;
        float additionalSpacing = (int)(spacing - 50) * glyphFont.Metrics.WhitespaceWidth / 50f;

        char previousCharacter = '\0';
        
        for (var idx = 0; idx < text.Length; ++idx)
        {
            var c = text[idx];
            var glyph = glyphFont.GetGlyph(c);

            float advance = glyphFont.Metrics.WhitespaceWidth + additionalSpacing;
            
            if (glyph != null)
            {
                advance = glyph.Advance + glyph.GetKerning(previousCharacter) + additionalSpacing;
                previousCharacter = c;
            }
            else
            {
                previousCharacter = '\0';
            }

            width += advance;
        }

        return width;
    }
    
    public static Size MeasureText(IGlyphFont glyphFont, TextSource text, TextSpacing spacing)
    {
        float width = 0;

        float maxWidth = 0;
        int lines = 0;
        
        float additionalSpacing = (int)(spacing - 50) * glyphFont.Metrics.WhitespaceWidth / 50f;

        char previousCharacter = '\0';
        
        for (var idx = 0; idx < text.Length; ++idx)
        {
            if (text[idx] == '\n')
            {
                maxWidth = Math.Max(width, maxWidth);
                width = 0;
                lines++;
                previousCharacter = '\0';
                continue;
            }
            var c = text[idx];
            var glyph = glyphFont.GetGlyph(c);

            float advance = glyphFont.Metrics.WhitespaceWidth + additionalSpacing;
            
            if (glyph != null)
            {
                advance = glyph.Advance + glyph.GetKerning(previousCharacter) + additionalSpacing;
                previousCharacter = c;
            }
            else
            {
                previousCharacter = '\0';
            }

            width += advance;
        }

        lines++;
        
        maxWidth = Math.Max(width, maxWidth);
        return new Size((int)MathF.Ceiling(maxWidth), lines * glyphFont.Metrics.LineHeight);
    }
}