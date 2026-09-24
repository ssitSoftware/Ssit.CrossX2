using Ssit.CrossX2.Framework.Graphics.Misc;
using Ssit.CrossX2.Framework.IO;
using Ssit.CrossX2.Framework.IoC;
using Ssit.CrossX2.Framework.Utils;

namespace Ssit.CrossX2.Framework.Graphics.Internal;

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
                    int fillAlpha = color.G * color.A / 255;

                    if (fillAlpha > 192)
                    {
                        outlineAlpha = 255 - (fillAlpha - 128) * 2;
                        outlineAlpha = Math.Max(0, outlineAlpha) * color.A / 255;
                        outlineAlpha = Math.Min(255, outlineAlpha);
                    }
                    
                    fillColors[x, y] = new RgbaColor(255, 255, 255, (byte)fillAlpha);
                    outlineColors[x, y] = new RgbaColor(255, 255, 255, (byte)outlineAlpha);
                }
            }
        }

        ITexture outlineSheet;
        using (var outlineStream = ImagesUtility.GetStream(outlineColors))
        {
            outlineSheet = container.IoCConstruct<ITexture>(new LoadTextureParameters
            {
                DiffuseMapStream = outlineStream,
                GlowFromDiffuse = true
            });
        }
        
        ITexture fillSheet;
        using (var fillStream = ImagesUtility.GetStream(fillColors))
        {
            fillSheet = container.IoCConstruct<ITexture>(new LoadTextureParameters
            {
                DiffuseMapStream = fillStream,
                GlowFromDiffuse = true
            });
        }
        
        return (fillSheet, outlineSheet);
    }
}