using System;
using SkiaSharp;
using Ssit.CrossX2.Editor.Helpers;
using Ssit.CrossX2.Framework.Games.Template;

namespace Ssit.CrossX2.Editor.Service;

public class EditorBitmapsProvider : IEditorBitmapsProvider
{
    public SKImage[] MaterialsPreview { get; }

    public EditorBitmapsProvider(IGameTemplate gameTemplate)
    {
        var previews = new SKImage[gameTemplate.Materials.Length];
        using var typeface = SKTypeface.FromFamilyName("Arial Black");
        
        for (var idx = 1; idx < gameTemplate.Materials.Length; ++idx)
        {
            var material = gameTemplate.Materials[idx];
            
            var font = new SKFont(typeface, 48);
            var skPaint = new SKPaint(font);
            
            using var bmp = new SKBitmap(128, 128, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            using var canvas = new SKCanvas(bmp);
            
            canvas.Clear(material.PreviewColor.ToSkia().WithAlpha(64));

            var lines = material.ShortName.Split('|');

            float width;
            
            var size = 48;
            while (true)
            {
                width = 0;
                foreach (var txt in lines)
                {
                    width = Math.Max(width, skPaint.MeasureText(txt));
                }

                if (width < 120)
                    break;
                
                skPaint.Dispose();
                font.Dispose();

                size -= 2;
                font = new SKFont(typeface, size);
                skPaint = new SKPaint(font);
            }

            var height = lines.Length * font.Size;
            skPaint.IsAntialias = true;
            skPaint.Color = material.PreviewColor.ToSkia();
            skPaint.IsStroke = false;

            var y = (128 - height) / 2 + font.Size;
            foreach (var txt in lines)
            {
                width = skPaint.MeasureText(txt);
                var x = (128 - width) / 2;
                canvas.DrawText(txt, x, y, skPaint);
                y += font.Size;
            }

            y = (128 - height) / 2 + font.Size;
            skPaint.Color = SKColors.Black;
            skPaint.IsStroke = true;
            skPaint.StrokeWidth = 4;
            
            foreach (var txt in lines)
            {
                width = skPaint.MeasureText(txt);
                var x = (128 - width) / 2;
                canvas.DrawText(txt, x, y, skPaint);
                y += font.Size;
            }

            previews[idx] = SKImage.FromBitmap(bmp);

            font.Dispose();
            skPaint.Dispose();
        }

        MaterialsPreview = previews;
    }
}