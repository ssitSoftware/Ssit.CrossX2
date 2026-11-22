using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using SkiaSharp;
using Ssit.CrossX2.Editor.Helpers;
using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.Games.Map;
using Ssit.CrossX2.Framework.Graphics.Sprites;

namespace Ssit.CrossX2.Editor.Service;

/// <summary>
/// Extracts a region of the map as a <see cref="PrefabFile"/> and renders its preview image.
/// </summary>
public class PrefabBuilder
{
    private const float SpeedEpsilon = 0.0001f;
    private const int MaxPreviewUpscale = 8;

    private readonly IEditorInstances _instances;

    public PrefabBuilder(IEditorInstances instances)
    {
        _instances = instances;
    }

    /// <summary>Layers with the same parallax speed as <paramref name="layer"/> (itself included), in map order.</summary>
    public IReadOnlyList<MapLayer> GetMatchingLayers(MapLayer layer)
    {
        return _instances.Map.Layers
            .Where(l => l == layer ||
                        (MathF.Abs(l.HorizontalSpeed - layer.HorizontalSpeed) < SpeedEpsilon &&
                         MathF.Abs(l.VerticalSpeed - layer.VerticalSpeed) < SpeedEpsilon))
            .ToList();
    }

    /// <summary>
    /// Copies the tiles of <paramref name="region"/> (in tiles of <paramref name="sourceLayer"/>) and, unless
    /// <paramref name="includeObjects"/> is false, the objects intersecting it. Layers with nothing extracted are
    /// left out. With more than one part left the prefab is bound to the layers it came from, otherwise it's
    /// layer-agnostic.
    /// </summary>
    public PrefabFile Build(Rectangle region, MapLayer sourceLayer, bool allMatchingLayers, bool includeObjects = true)
    {
        var map = _instances.Map;
        var layers = allMatchingLayers ? GetMatchingLayers(sourceLayer) : new[] { sourceLayer };

        var prefab = new PrefabFile(map.TemplateId, map.TileSize, map.Tilesets, region.Width, region.Height);

        foreach (var layer in layers)
        {
            // Layers with the same speed scroll together aligned to their bottom edge (see LayerOffsetCalculator):
            // tile row y of the source layer is row y + (height difference) of the other layer.
            var x0 = region.X;
            var y0 = region.Y + layer.Height - sourceLayer.Height;

            var part = ExtractPart(layer, x0, y0, region.Width, region.Height, includeObjects);
            if (part is not null)
            {
                prefab.Layers.Add(part);
            }
        }

        prefab.IsLayerBound = prefab.Layers.Count > 1;
        return prefab;
    }

    private MapLayer ExtractPart(MapLayer layer, int x0, int y0, int width, int height, bool includeObjects)
    {
        var part = new MapLayer(layer.Id, width, height, _instances.Template)
        {
            Name = layer.Name,
            Depth = layer.Depth,
            HorizontalSpeed = layer.HorizontalSpeed,
            VerticalSpeed = layer.VerticalSpeed,
            TintColor = layer.TintColor,
            FogColor = layer.FogColor,
            UseAmbientLightColor = layer.UseAmbientLightColor
        };

        var hasContent = false;

        for (var x = 0; x < width; ++x)
        {
            var sx = x0 + x;

            for (var y = 0; y < height; ++y)
            {
                var sy = y0 + y;

                var tile = sx >= 0 && sy >= 0 && sx < layer.Width && sy < layer.Height
                    ? layer.Tiles[sx, sy]
                    : Tile.Empty;

                part.Tiles[x, y] = tile;
                hasContent |= !tile.IsEmpty;
            }
        }

        var area = new RectangleF(x0, y0, width, height);
        var origin = new Vector2(x0, y0);

        var objects = includeObjects ? layer.Objects : Enumerable.Empty<MapObject>();

        foreach (var obj in objects)
        {
            var bounds = GetObjectBounds(obj);
            var inside = bounds.HasValue ? bounds.Value.IsIntersecting(area) : area.Contains(obj.Position);

            if (!inside) continue;

            var copy = obj.Clone(_instances.Template);
            copy.Position -= origin;

            part.Objects.Add(copy);
            hasContent = true;
        }

        part.SortObjects();

        return hasContent ? part : null;
    }

    /// <summary>
    /// Bounds of the object's editor image in tiles (map space), like the editor draws it;
    /// null when the image or its sequence is missing.
    /// </summary>
    public RectangleF? GetObjectBounds(MapObject obj)
    {
        if (!TryGetFrame(obj, out _, out var frame, out var origin))
            return null;

        float ts = _instances.Template.TileSize;

        var left = obj.Flipped ? -(frame.Source.Width - origin.X) : -origin.X;

        return new RectangleF(obj.Position.X + left / ts, obj.Position.Y - origin.Y / ts,
            frame.Source.Width / ts, frame.Source.Height / ts);
    }

    /// <summary>
    /// Renders all parts as PNG: objects behind the tiles, tiles, objects in front, each layer with its tint.
    /// The image covers the prefab plus whatever sticks out of it, fitted into <paramref name="maxSize"/>
    /// (small prefabs are scaled up by whole pixels, so pixel art stays sharp). Null for an empty prefab.
    /// </summary>
    public byte[] RenderPreview(PrefabFile prefab, int maxSize)
    {
        if (prefab.Layers.Count == 0) return null;

        var ts = _instances.Template.TileSize;

        var minX = 0f;
        var minY = 0f;
        var maxX = (float)prefab.Width;
        var maxY = (float)prefab.Height;

        foreach (var obj in prefab.Layers.SelectMany(l => l.Objects))
        {
            var bounds = GetObjectBounds(obj);
            if (!bounds.HasValue) continue;

            minX = MathF.Min(minX, bounds.Value.X);
            minY = MathF.Min(minY, bounds.Value.Y);
            maxX = MathF.Max(maxX, bounds.Value.Right);
            maxY = MathF.Max(maxY, bounds.Value.Bottom);
        }

        var pixelWidth = (maxX - minX) * ts;
        var pixelHeight = (maxY - minY) * ts;

        var scale = MathF.Min(maxSize / pixelWidth, maxSize / pixelHeight);
        var pixelArt = scale >= 1;

        if (pixelArt)
        {
            scale = MathF.Min(MaxPreviewUpscale, MathF.Floor(scale));
        }

        var width = Math.Max(1, (int)MathF.Ceiling(pixelWidth * scale));
        var height = Math.Max(1, (int)MathF.Ceiling(pixelHeight * scale));

        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;

        canvas.Clear(SKColors.Transparent);
        canvas.Scale(scale);
        canvas.Translate(-minX * ts, -minY * ts);

        using var paint = new SKPaint();
        paint.FilterQuality = pixelArt ? SKFilterQuality.None : SKFilterQuality.Medium;

        foreach (var part in prefab.Layers)
        {
            using var tint = part.TintColor != RgbaColor.White ? CreateTintFilter(part.TintColor) : null;
            paint.ColorFilter = tint;

            DrawObjects(canvas, part, paint, behind: true);
            DrawTiles(canvas, part, paint);
            DrawObjects(canvas, part, paint, behind: false);

            paint.ColorFilter = null;
        }

        canvas.Flush();

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        return data.ToArray();
    }

    private void DrawTiles(SKCanvas canvas, MapLayer part, SKPaint paint)
    {
        var ts = _instances.Template.TileSize;
        var tilesets = _instances.TilesetsContainer.TileSets;

        const float epsilon = 0.0001f;

        for (var x = 0; x < part.Width; ++x)
        {
            for (var y = 0; y < part.Height; ++y)
            {
                var tile = part.Tiles[x, y];
                if (tile.IsEmpty || tile.TileSet >= tilesets.Length) continue;

                var image = tilesets[tile.TileSet].RasterImage;
                if (image is null) continue;

                var src = SKRect.Create(tile.X * ts + epsilon, tile.Y * ts + epsilon, ts - epsilon * 2, ts - epsilon * 2);
                var dest = SKRect.Create(x * ts, y * ts, ts, ts);

                canvas.DrawImage(image, src, dest, paint);
            }
        }
    }

    private void DrawObjects(SKCanvas canvas, MapLayer part, SKPaint paint, bool behind)
    {
        var ts = _instances.Template.TileSize;

        foreach (var obj in part.Objects)
        {
            if (behind != obj.ZOrder < 0) continue;
            if (!TryGetFrame(obj, out var editorImage, out var frame, out var origin)) continue;

            var image = editorImage.Sprite.RasterImage;
            if (image is null) continue;

            var pos = obj.Position * ts;

            var src = SKRect.Create(frame.Source.X, frame.Source.Y, frame.Source.Width, frame.Source.Height);
            var dest = SKRect.Create(pos.X - origin.X, pos.Y - origin.Y, frame.Source.Width, frame.Source.Height);

            if (obj.Flipped)
            {
                canvas.Save();
                canvas.Scale(-1, 1, pos.X, 0);
                canvas.DrawImage(image, src, dest, paint);
                canvas.Restore();
            }
            else
            {
                canvas.DrawImage(image, src, dest, paint);
            }
        }
    }

    private bool TryGetFrame(MapObject obj, out EditorImage editorImage,
        out Sprite.SpriteFrame frame, out Vector2 origin)
    {
        frame = null;
        origin = Vector2.Zero;

        try
        {
            editorImage = obj.HasLogic
                ? _instances.ObjectsContainer.Get(obj.TypeId)
                : _instances.ImagesContainer.Get(obj.TypeId);

            frame = editorImage.Sprite.GetFrameForTime(editorImage.Sequence, 0);
            origin = frame.Offset + editorImage.Sprite.Origin;
            return true;
        }
        catch (Exception e) when (e is KeyNotFoundException or InvalidDataException)
        {
            editorImage = null;
            return false;
        }
    }

    private static SKColorFilter CreateTintFilter(RgbaColor color)
    {
        return SKColorFilter.CreateColorMatrix(
        [
            color.Rf, 0, 0, 0, 0,
            0, color.Gf, 0, 0, 0,
            0, 0, color.Bf, 0, 0,
            0, 0, 0, color.Af, 0
        ]);
    }
}
