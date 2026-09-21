using System.Numerics;
using Newtonsoft.Json;
using SkiaSharp;
using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.Graphics.Internal;
using Ssit.CrossX2.Framework.Xml;

namespace Ssit.CrossX.Tool;

internal class XmlToFontConverter(string fullPath, XNode xmlNode) : IXmlFileConverter
{
    private class FontsInfo
    {
        public string[] Fonts { get; set; }
    }
    
    [Flags]
    private enum CharSets
    {
        Ascii = 1,
        Polish = 2,
        German = 4,
        French = 8,
        Spanish = 16,
        Special = 32,
        Dos = 64,
        Icons = 128
    }

    private const string PolishCharacters = "ĄĆĘŁŃÓŚŹŻąćęłńóśźż";
    private const string GermanCharacters = "ÄäÖöÜüẞß";
    private const string FrenchCharacters = "ÀÂÄÆÇÈÉÊËÎÏÔŒÙÛÜèéêëîïôœùûüàâäæç";
    private const string SpanishCharacters = "ÁÉÍÑÓÚáéíñóú";
    private const string SpecialCharacters = "®©™€§£";
    private const string DosCharacters = "⌂ ¡¢£¤¥¦§¨©ª«¬-®¯°±²³´µ¶·¸¹º»¼½¾¿ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞßàáâãäåæçèéêëìíîïðñòóôõö÷øùúûüýþÿĀāĂăĄąĆćĈĉĊċČčĎďĐđĒēĔĕĖėĘęĚěĜĝĞğĠġĢģĤĥĦħĨĩĪīĬĭĮįİıĲĳĴĵĶķĸĹĺĻļĽľĿŀŁłŃńŅņŇňŉŊŋŌōŎŏŐőŒœŔŕŖŗŘřŚśŜŝŞşŠšŢţŤťŦŧŨũŪūŬŭŮůŰűŲųŴŵŶŷŸŹźŻżŽžſƒơƷǺǻǼǽǾǿȘșȚțɑɸˆˇˉ˘˙˚˛˜˝;΄΅Ά·ΈΉΊΌΎΏΐΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩΪΫάέήίΰαβγδεζηθικλμνξοπρςστυφχψωϊϋόύώϐϴЀЁЂЃЄЅІЇЈЉЊЋЌЍЎЏАБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдежзийклмнопрстуфхцчшщъыьэюяѐёђѓєѕіїјљњћќѝўџҐґ־אבגדU+05D3הוזחטיךכלםמןנסעףפץצקרשתװױײ׳״ᴛᴦᴨẀẁẂẃẄẅẟỲỳ‐‒–—―‗‘’‚‛“”„‟†‡•…‧‰′″‵‹›‼‾‿⁀⁄⁔⁴⁵⁶⁷⁸⁹⁺⁻ⁿ₁₂₃₄₅₆₇₈₉₊₋₣₤₧₪€℅ℓ№™Ω℮⅐⅑⅓⅔⅕⅖⅗⅘⅙⅚⅛⅜⅝⅞←↑→↓↔↕↨∂∅∆∈∏∑−∕∙√∞∟∩∫≈≠≡≤≥⊙⌀⌂⌐⌠⌡─│┌┐└┘├┤┬┴┼═║╒╓╔╕╖╗╘╙╚╛╜╝╞╟╠╡╢╣╤╥╦╧╨╩╪╫╬▀▁▄█▌▐░▒▓■□▪▫▬▲►▼◄◊○●◘◙◦☺☻☼♀♂♠♣♥♦♪♫✓ﬁﬂ";
    private const string IconsCharacters = "\xf6a2";
        
    
    public async Task Generate()
    
    {
        var list = new List<string>();
        
        foreach (var cn in xmlNode.Nodes)
        {
            if (cn.Tag != "Font")
                continue;

            var paths = await GenerateFont(cn);
            list.AddRange(paths);
        }

        var fontsInfo = new FontsInfo
        {
            Fonts = list.ToArray()
        };
        
        var json = JsonConvert.SerializeObject(fontsInfo, Formatting.Indented);
        var filePath = Path.Combine(Path.GetDirectoryName(fullPath) ?? "", Path.GetFileNameWithoutExtension(fullPath) ?? "") + ".json";
        
        await File.WriteAllTextAsync(filePath, json);
    }

    private async Task<List<string>> GenerateFont(XNode node)
    {
        var sizes = node.Attribute("Sizes")?.Split(',');

        if (sizes == null)
            return null;

        XNodeAttributes attr = new XNodeAttributes(node);

        var family = node.Attribute("Family") ?? "";
        var weight = attr.AsEnum("Weight", SKFontStyleWeight.Normal);
        var style = attr.AsEnum("Style", SKFontStyleWidth.Normal);
        var slant = attr.AsBoolean("Italic", false) ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;

        using var typeface = SKTypeface.FromFamilyName(family, weight, style, slant);
        
        var list = new List<string>();
        
        foreach (var size in sizes)
        {
            if (int.TryParse(size, out var sizeInt))
            {
                list.Add(await GenerateFont(node, typeface, sizeInt));
            }
        }

        return list;
    }

    private async Task<string> GenerateFont(XNode node, SKTypeface typeface, int size)
    {
        var name = node.Attribute("Name");
        var fileName = name + $"@{size}";

        var outputData = Path.GetDirectoryName(fullPath) + $"/{fileName}.font";
        var outputBitmap = Path.GetDirectoryName(fullPath) + $"/{fileName}.png";

        File.Delete(outputData);
        File.Delete(outputBitmap);

        await Task.Delay(10);

        var attr = new XNodeAttributes(node);
        var antialiasing = attr.AsBoolean("Antialiasing", true);

        using var font = new SKFont(typeface, size);
        using var paint = new SKPaint(font);

        paint.IsAntialias = antialiasing;
        paint.HintingLevel = SKPaintHinting.Full;
        paint.SubpixelText = false;
        paint.IsStroke = false;

        using var targetBitmap = new SKBitmap(new SKImageInfo(size * 4, size * 4));
        using var glyphCanvas = new SKCanvas(targetBitmap);

        var chars = new char[2];
        var characters = GetCharacters(attr.AsEnum("CharSets", CharSets.Ascii));

        var glyphs = new List<(Glyph, SKBitmap)>();
        
        var outline = (float)attr.AsPercentOrScalar("Outline", size, 0);
        
        SKColor outlineColor = SKColors.Black;
        SKColor fillColor = SKColors.White;
        
        foreach (var character in characters)
        {
            glyphCanvas.Clear(SKColors.Transparent);

            var chText = character.ToString();

            if (outline > 0)
            {
                paint.IsStroke = true;
                paint.StrokeWidth = outline;
                paint.StrokeCap = SKStrokeCap.Round;
                paint.StrokeJoin = SKStrokeJoin.Round;
                paint.Color = outlineColor;
                
                glyphCanvas.DrawText(chText, new SKPoint(size, size * 2), paint);
            }

            paint.Color = fillColor;
            paint.IsStroke = false;
            
            glyphCanvas.DrawText(chText, new SKPoint(size, size * 2), paint);

            chars[1] = character;
            
            var (croppedBitmap, offset) = CropGlyph(targetBitmap, new SKPoint(size, size * 2));

            var currentWidth = paint.MeasureText(chText);
            
            Dictionary<char, float> kerning = null;
            
            for (var idx = 0; idx < characters.Count; idx++)
            {
                chars[0] = characters[idx];
                var prevWidth = paint.MeasureText(new ReadOnlySpan<char>(chars, 0, 1));
                var bothWidth = paint.MeasureText(new ReadOnlySpan<char>(chars, 0, 2));

                if (MathF.Abs(prevWidth + currentWidth - bothWidth) > float.Epsilon)
                {
                    kerning ??= new Dictionary<char, float>();
                    kerning.Add(chars[0], bothWidth - currentWidth - prevWidth);
                }
            }

            glyphs.Add((new Glyph(character, new Rectangle(0,0,0,0), offset, currentWidth, kerning), croppedBitmap));
        }

        var sheetWidth = (int)Math.Ceiling(attr.AsDouble("SheetWidthFactor", 25.6) * size);
        
        var spacing = (int)Math.Ceiling(attr.AsPercentOrScalar("Spacing", size, 0));
        
        var fontSheet = CreateFontSheet(glyphs, spacing, sheetWidth);

        var whitespaceWidth = (int)MathF.Ceiling(paint.MeasureText(" "));
        var ascender = (int)MathF.Ceiling(font.Metrics.Ascent);
        var descender = (int)MathF.Ceiling(font.Metrics.Descent);
        var capHeight = (int)MathF.Ceiling(font.Metrics.CapHeight);
        var xHeight = (int)MathF.Ceiling(font.Metrics.XHeight);
        var lineHeight = (int)MathF.Ceiling(-font.Metrics.Ascent + font.Metrics.Descent + font.Metrics.Leading);
        
        var metrics = new GlyphFont.FontMetrics(capHeight, xHeight, ascender, descender, lineHeight, whitespaceWidth);
        var fontOut = new GlyphFont(name, size, outline > 0, metrics, glyphs.Select(g => g.Item1).ToArray());

        await using var outStream = new FileStream(outputData, FileMode.Create, FileAccess.Write);
        fontOut.Save(outStream);
        await outStream.FlushAsync();

        var data = fontSheet.Encode(SKEncodedImageFormat.Png, 0);
        
        await using var stream = File.OpenWrite(outputBitmap);
        data.SaveTo(stream);
        await stream.FlushAsync();

        return $"{fileName}.font";
    }

    private SKBitmap CreateFontSheet(List<(Glyph, SKBitmap)> glyphs, int spacing, int maxSheetWidth)
    {
        var mapping = new List<(Glyph, Rectangle)>();
        var bmp = AtlasTool.CreateAtlas(glyphs, mapping, spacing, maxSheetWidth);

        foreach (var map in mapping)
        {
            map.Item1.Source = map.Item2;
        }

        return bmp;
    }

    private (SKBitmap croppedBitmap, Vector2 offset) CropGlyph(SKBitmap glyph, SKPoint point)
    {
        var left = 0;
        var top = 0;
        var right = 0;
        var bottom = 0;

        for (var y = 0; y < glyph.Height; y++)
        {
            bool hasPixels = false;
            for (var x = 0; x < glyph.Width; x++)
            {
                if (glyph.GetPixel(x, y).Alpha > 0)
                {
                    hasPixels = true;
                    break;
                }
            }

            if (hasPixels)
            {
                break;
            }

            top++;
        }

        for (var y = glyph.Height-1; y >= 0; y--)
        {
            bool hasPixels = false;
            for (var x = 0; x < glyph.Width; x++)
            {
                if (glyph.GetPixel(x, y).Alpha > 0)
                {
                    hasPixels = true;
                    break;
                }
            }

            if (hasPixels)
            {
                break;
            }

            bottom++;
        }
        
        for (var x = 0; x < glyph.Width; x++)
        {
            bool hasPixels = false;
            for (var y = 0; y < glyph.Height; y++)
            {
                if (glyph.GetPixel(x, y).Alpha > 0)
                {
                    hasPixels = true;
                    break;
                }
            }

            if (hasPixels)
            {
                break;
            }

            left++;
        }

        for (var x = glyph.Width-1; x >= 0; x--)
        {
            bool hasPixels = false;
            for (var y = 0; y < glyph.Height; y++)
            {
                if (glyph.GetPixel(x, y).Alpha > 0)
                {
                    hasPixels = true;
                    break;
                }
            }

            if (hasPixels)
            {
                break;
            }

            right++;
        }

        if (right == glyph.Width || bottom == glyph.Height)
        {
            return (null, Vector2.Zero);
        }
        
        var width = glyph.Width - right - left;
        var height = glyph.Height - bottom - top;

        var croppedBitmap = new SKBitmap(width, height);

        using (var glyphCanvas = new SKCanvas(croppedBitmap))
        {
            glyphCanvas.Clear(SKColors.Transparent);
            glyphCanvas.DrawBitmap(glyph, -left, -top);
        }

        return (croppedBitmap, new Vector2(left - point.X, top - point.Y));
    }

    private List<char> GetCharacters(CharSets charsets)
    {
        HashSet<char> set = new();
        
        if (charsets.HasFlag(CharSets.Ascii))
        {
            for (var idx = 33; idx < 128; ++idx)
            {
                var c = (char)idx;
                set.Add(c);
            }
        }

        if (charsets.HasFlag(CharSets.Polish))
        {
            foreach (var c in PolishCharacters)
            {
                set.Add(c);
            }
        }

        if (charsets.HasFlag(CharSets.German))
        {
            foreach (var c in GermanCharacters)
            {
                set.Add(c);
            }
        }

        if (charsets.HasFlag(CharSets.French))
        {
            foreach (var c in FrenchCharacters)
            {
                set.Add(c);
            }
        }
        
        if (charsets.HasFlag(CharSets.Spanish))
        {
            foreach (var c in SpanishCharacters)
            {
                set.Add(c);
            }
        }
        
        if (charsets.HasFlag(CharSets.Special))
        {
            foreach (var c in SpecialCharacters)
            {
                set.Add(c);
            }
        }
        
        if (charsets.HasFlag(CharSets.Icons))
        {
            foreach (var c in IconsCharacters)
            {
                set.Add(c);
            }
        }
        
        if (charsets.HasFlag(CharSets.Dos))
        {
            foreach (var c in DosCharacters)
            {
                set.Add(c);
            }
        }

        foreach (var c in xmlNode.Attribute("ExtraCharacters") ?? "")
        {
            set.Add(c);
        }

        return set.ToList();
    }
}
