using System.Numerics;
using Ssit.CrossX2.Framework.IO;
using Ssit.CrossX2.Framework.Utils;

namespace Ssit.CrossX2.Framework.Graphics.Sprites;

public class SpriteCollider(byte[,] mask) : IDisposable
{
    public Size Size => new Size(mask.GetLength(0), mask.GetLength(1));
    
    public static SpriteCollider Load(string path, IFilesProvider filesProvider)
    {
        var maskStream = filesProvider.Open(path);
        var mask = ImagesUtility.LoadIndexedImage(maskStream, [RgbaColor.Transparent, RgbaColor.Black, RgbaColor.Red, RgbaColor.Green, RgbaColor.Blue, RgbaColor.Yellow, RgbaColor.Cyan, RgbaColor.Magenta]);
        
        var w = mask.GetLength(0);
        var h = mask.GetLength(1);

        for (var x = 0; x < w; ++x)
        {
            for (var y = 0; y < h; ++y)
            {
                if (mask[x, y] > 0)
                {
                    mask[x, y] = (byte)(1 << (mask[x, y] - 1));
                }
            }
        }
        
        return new SpriteCollider(mask);
    }
    
    private byte GetMaskAt(float fx, float fy, int scale)
    {
        int x = (int)(fx / scale + 0.5f);
        int y = (int)(fy / scale + 0.5f);

        if (x < 0) return 0;
        if (y < 0) return 0;
        
        var maxX = mask.GetLength(0);
        var maxY = mask.GetLength(1);
        
        if (x >= maxX) return 0;
        if (y >= maxY) return 0;
        
        return mask[x, y];
    }
    
    public MaskIndex CheckCollision(RectangleF area, SpriteCollider other, RectangleF otherArea, Vector2 offset, int scale, int otherScale, MaskIndex maskIndex = MaskIndex.All)
    {
        var width = area.Width;
        var height = area.Height;

        MaskIndex value = 0;
        
        for (var xx = 0; xx < width; xx += scale)
        {
            var checkX = xx + offset.X;
            
            if (checkX < 0) continue;
            if (checkX >= otherArea.Width) continue;
            checkX += otherArea.X;
            
            for (var yy = 0; yy < height; yy += scale)
            {
                var v1 = GetMaskAt(area.X + xx, area.Y + yy, scale);
                if ((v1 & (byte)maskIndex) == 0)
                {
                    continue;
                }
                
                var checkY = yy + offset.Y;

                if (checkY < 0) continue;
                if (checkY >= otherArea.Height) continue;
                checkY += otherArea.Y;
                
                var v2 = other.GetMaskAt(checkX, checkY, otherScale);
                value |= (MaskIndex)v2;
            }
        } 
        
        return value;
    }
    
    void IDisposable.Dispose()
    {
    }
}