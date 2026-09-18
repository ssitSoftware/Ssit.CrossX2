using Ssit.CrossX2.Graphics.Misc;
using Ssit.CrossX2.IO;
using Ssit.CrossX2.IoC;
using Ssit.CrossX2.Utils;

namespace Ssit.CrossX2.Graphics.Internal;

public static class TextureHelper
{
    public static (ITexture, ITexture) LoadComplexSheet(IFilesProvider filesProvider, IIoCContainer container, string path)
    {
        using var stream = filesProvider.Open(path);
        var colors = ImagesUtility.LoadImage(stream);
        
        var fillColors = new RgbaColor[colors.GetLength(0), colors.GetLength(1)];
        var outlineColors = new RgbaColor[colors.GetLength(0), colors.GetLength(1)];

        for (var x = 0; x < colors.GetLength(0); x++)
        {
            for (var y = 0; y < colors.GetLength(1); y++)
            {
                var color = colors[x, y];
                
                if (color.A > 0)
                {
                    int outlineAlpha = color.A;
                    int fillAlpha = color.G;

                    if (fillAlpha > 128)
                    {
                        outlineAlpha = 255 - (fillAlpha - 128) * 2;
                        outlineAlpha = Math.Max(0, outlineAlpha) * color.A / 255;
                        outlineAlpha = Math.Min(255, outlineAlpha);
                    }
                    
                    fillColors[x, y] = RgbaColor.FromNonPremultiplied(255, 255, 255, (byte)fillAlpha);
                    outlineColors[x, y] = RgbaColor.FromNonPremultiplied(255, 255, 255, (byte)outlineAlpha);
                }
            }
        }

        ITexture outlineSheet;
        using (var outlineStream = ImagesUtility.GetStream(outlineColors))
        {
            outlineSheet = container.IoCConstruct<ITexture>(new LoadTextureParameters
            {
                DiffuseMapStream = outlineStream
            });
        }
        
        ITexture fillSheet;
        using (var fillStream = ImagesUtility.GetStream(fillColors))
        {
            fillSheet = container.IoCConstruct<ITexture>(new LoadTextureParameters
            {
                DiffuseMapStream = fillStream
            });
        }
        
        return (fillSheet, outlineSheet);
    }
}