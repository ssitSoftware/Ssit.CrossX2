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

        private SKImage _mask;
        private SKImage _hardwareMask;

        public string Name { get; }
        public string Path { get; }

        public TilesetMeta Meta { get; }

        public bool HasMeta => Meta != null;
        public bool HasMask => _mask != null;

        private Dictionary<(int, int), (SKPoint[], SKPoint[])> _collissions = new();

        public Tileset(SKImage image, TilesetMeta meta, SKImage mask, string path)
        {
            Name = System.IO.Path.GetFileNameWithoutExtension(path).Replace("_", " ");
            Path = path;
            Meta = meta;
            _image = image;
            _mask = mask;

            if (meta != null)
            {
                foreach (var col in meta.Collisions)
                {
                    _collissions.Add(col.Key, (col.Value.Item1.Select(o => o.ToSkia()).ToArray(), new SKPoint[col.Value.Item1.Length+1]));
                }
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

        public SKImage GetMask(GRContext context)
        {
            if (_mask is null)
            {
                return null;
            }

            if (_hardwareMask?.Handle is not null)
            {
                return _hardwareMask;
            }

            if (context == null)
            {
                return _mask;
            }

            _hardwareMask = _mask.ToTextureImage(context);
            return _hardwareMask;
        }

        public void DrawMask(SKCanvas canvas, GRContext context, int tileX, int tileY, int tileSize, SKRect dest, SKColor tint)
        {
            var mask = GetMask(context);
            if (mask is null) return;

            const float epsilon = 0.0001f;
            var src = SKRect.Create(tileX * tileSize + epsilon, tileY * tileSize + epsilon,
                tileSize - epsilon * 2, tileSize - epsilon * 2);

            using var filter = SKColorFilter.CreateBlendMode(tint, SKBlendMode.SrcIn);
            using var paint = new SKPaint { ColorFilter = filter };

            canvas.DrawImage(mask, src, dest, paint);
        }
    }
}