using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using Ssit.CrossX2.Framework.Games;

namespace Ssit.CrossX2.Editor.Helpers
{
    public class Tileset
    {
        private SKImage _image;
        private SKImage _hardwareImage;
        
        public string Name { get; }
        public string Path { get; }
        
        public TilesetMeta Meta { get; }

        private Dictionary<(int, int), (SKPoint[], SKPoint[])> _collissions = new();
        
        public Tileset(SKImage image, TilesetMeta meta, string path)
        {
            Name = System.IO.Path.GetFileNameWithoutExtension(path).Replace("_", " ");
            Path = path;
            Meta = meta;
            _image = image;

            foreach (var col in meta.Collisions)
            {
                _collissions.Add(col.Key, (col.Value.Item1.Select(o => o.ToSkia()).ToArray(), new SKPoint[col.Value.Item1.Length+1]));
            }
        }

        public bool GetCollissionPolygon(int x, int y, float scale, float offsetX, float offsetY, out SKPoint[] result)
        {
            result = null;

            if (_collissions.TryGetValue((x, y), out var res))
            {
                for (var idx = 0; idx < res.Item2.Length; ++idx)
                {
                    res.Item2[idx] = new SKPoint(res.Item1[idx % res.Item1.Length].X * scale + offsetX,
                        res.Item1[idx % res.Item1.Length].Y * scale + offsetY);
                }

                result = res.Item2;
                return true;
            }

            return false;
        }

        public SKImage Get(GRContext context)
        {
            if (_hardwareImage?.Handle is not null)
            {
                return _hardwareImage;
            }

            if (context == null)
            {
                return _image;
            }

            _hardwareImage = _image.ToTextureImage(context);
            return _hardwareImage;
        }
    }
}