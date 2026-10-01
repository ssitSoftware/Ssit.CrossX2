using Ssit.CrossX2.Framework.Graphics.Misc;
using Ssit.CrossX2.Framework.IO;
using Ssit.CrossX2.Framework.IoC;
using Ssit.CrossX2.Framework.Utils;

namespace Ssit.CrossX2.Framework.Graphics;

public class TextureArrayLoader(IFilesProvider filesProvider, IIoCContainer container, RgbaColor[] palette)
{
    private class Map
    {
        public RgbaColor[,] Colors;
    }

    public IDisposable Load(string path)
    {
        try
        {
            return LoadInternal(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw;
        }
    }
    
    private IDisposable LoadInternal(string path)
    {
        using var stream = filesProvider.Open(path);
        var colors = ImagesUtility.LoadImage(stream);
        
        var maps = new Map[palette.Length];
        
        for (var x = 0; x < colors.GetLength(0); x++)
        {
            for (var y = 0; y < colors.GetLength(1); y++)
            {
                var color = colors[x, y];

                if (color.A == 0)
                    continue;
                
                var dist = float.MaxValue;
                var index = -1;
                
                for (var idx = 0; idx < palette.Length; ++idx)
                {
                    var d = palette[idx].DistanceTo(color);
                    if (d < dist)
                    {
                        dist = d;
                        index = idx;
                    }
                }
                
                if (maps[index] is null)
                {
                    maps[index] = new Map
                    {
                        Colors = new RgbaColor[colors.GetLength(0), colors.GetLength(1)]
                    };
                }
                
                var map = maps[index];
                map.Colors[x, y] = RgbaColor.White;
            }
        }
        
        var textures = new ITexture[palette.Length];

        for (var idx = 0; idx < textures.Length; ++idx)
        {
            if (maps[idx] is null) continue;
            
            using (var outlineStream = ImagesUtility.GetStream(maps[idx].Colors))
            {
                textures[idx] = container.IoCConstruct<ITexture>(new LoadTextureParameters
                {
                    DiffuseMapStream = outlineStream,
                    GlowFromDiffuse = true
                });
            }
        }

        return new TextureArray(textures);
    }
}