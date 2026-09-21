using SkiaSharp;

namespace Ssit.CrossX.Tool;

public static class OneBitImageTool
{
    public static void ConvertTo1BitAndAddOutline(string path, SKColor foregroundColor, SKColor backgroundColor)
    {
        using var stream = File.Open(path, FileMode.Open);
        using var bmp = SKBitmap.Decode(stream);

        for (var x = 0; x < bmp.Width; x++)
        {
            for (var y = 0; y < bmp.Height; y++)
            {
                var clr = bmp.GetPixel(x, y); 
                if (clr.Alpha > 0 && clr != backgroundColor)
                {
                    bmp.SetPixel(x, y, foregroundColor);
                }
            }
        }
        
        for (var x = 0; x < bmp.Width; x++)
        {
            for (var y = 0; y < bmp.Height; y++)
            {
                if (bmp.GetPixel(x, y).Alpha == 0)
                {
                    if (CheckNeighbours(bmp, x, y, foregroundColor))
                    {
                        bmp.SetPixel(x, y, backgroundColor);
                    }
                }
            }
        }
        
        path = Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path.Replace("_to1bit.", "."))!) + ".png";
        using var outStream = File.Open(path, FileMode.Create);
        bmp.Encode(outStream, SKEncodedImageFormat.Png, 100);
    }

    private static bool CheckNeighbours(SKBitmap bmp, int x, int y, SKColor foregroundColor)
    {
        if (x > 0)
        {
            if (bmp.GetPixel(x - 1, y) ==  foregroundColor)
            {
                return true;
            }
        }
        
        if (y > 0)
        {
            if (bmp.GetPixel(x, y - 1) ==  foregroundColor)
            {
                return true;
            }
        }

        if (x < bmp.Width - 1)
        {
            if (bmp.GetPixel(x + 1, y) == foregroundColor)
            {
                return true;
            }
        }
        
        if (y < bmp.Height - 1)
        {
            if (bmp.GetPixel(x, y + 1) == foregroundColor)
            {
                return true;
            }
        }
        
        return false;
    }
}