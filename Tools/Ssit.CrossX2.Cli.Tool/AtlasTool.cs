using SkiaSharp;
using Ssit.CrossX2.Framework;

namespace Ssit.CrossX.Tool;

public static class AtlasTool
{
    public static SKBitmap CreateAtlas<T>(IReadOnlyList<(T item, SKBitmap bmp)> pieces, IList<(T, Rectangle)> mapping,
        int spacing = 2, int maxWidth = 2048, int heightFactor = 2)
    {
        var area = 0;

        foreach (var piece in pieces)
        {
            if (piece.bmp is null) continue;
            area += piece.bmp.Width * piece.bmp.Height;
        }
        
        using var tempBitmap = new SKBitmap(maxWidth, area / maxWidth * heightFactor);
        using var tempCanvas = new SKCanvas(tempBitmap);
        tempCanvas.Clear(SKColors.Transparent);
            
        var positionX = spacing;
        var positionY = spacing;

        var height = 0;
        var width = 0;

        foreach (var piece in pieces)
        {
            if (piece.bmp is null)
            {
                mapping.Add((piece.item, new Rectangle(0,0,0,0)));
                continue;
            }
            
            if (positionX + piece.bmp.Width > tempBitmap.Width)
            {
                positionY = height + spacing;
                positionX = spacing;
            }
            
            tempCanvas.DrawBitmap(piece.bmp, positionX, positionY);
            
            mapping.Add((piece.item, new Rectangle(positionX, positionY, piece.bmp.Width, piece.bmp.Height)));
            
            height = Math.Max(height, piece.bmp.Height + positionY);
            width = Math.Max(width, piece.bmp.Width + positionX);

            positionX += piece.bmp.Width + spacing;
            
            if (height > tempBitmap.Height)
            {
                mapping.Clear();
                return CreateAtlas(pieces, mapping, spacing, maxWidth, heightFactor + 1);
            }
        }

        height += spacing;
        width += spacing;
        
        var outBitmap = new SKBitmap(width, height);
        using var outCanvas = new SKCanvas(outBitmap);
        
        outCanvas.Clear(SKColors.Transparent);
        outCanvas.DrawBitmap(tempBitmap, SKPoint.Empty, new SKPaint
        {
            IsAntialias = true,
            BlendMode = SKBlendMode.Plus
        });

        return outBitmap;
    }
}