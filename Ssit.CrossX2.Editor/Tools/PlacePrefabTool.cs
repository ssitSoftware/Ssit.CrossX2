using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Avalonia.Input;
using SkiaSharp;
using Ssit.CrossX2.Editor.Helpers;
using Ssit.CrossX2.Editor.Input;
using Ssit.CrossX2.Editor.Service;
using Ssit.CrossX2.Framework.Games.Editor;
using Ssit.CrossX2.Framework.Games.Map;
using Ssit.CrossX2.Framework.Games.Template;

namespace Ssit.CrossX2.Editor.Tools;

/// <summary>
/// Places the prefab picked in the prefab library; its top-left corner goes to the tile under the cursor.
/// Layer rules: a single-layer prefab goes on the selected layer if it's the same kind (Main or not Main)
/// as the layer it came from; a multi-layer prefab puts each part on the layer it was extracted from, so the
/// selected layer has to be one of them. Empty prefab tiles keep the map's tiles. Placed objects get new ids;
/// links between them are kept, links to objects outside the prefab are cleared.
/// </summary>
public class PlacePrefabTool : EditorTool
{
    public new const string Name = "Place Prefab";

    private static readonly SKColor ValidColor = SKColors.GreenYellow;
    private static readonly SKColor InvalidColor = SKColors.Red;

    private readonly SKFont _font = new() { Size = 12 };

    private SKPointI? _anchor;

    // Computed on the UI thread, read by Render.
    private string _blockReason;

    /// <summary>The prefab to place; null when nothing is picked in the library.</summary>
    public PrefabFile Prefab
    {
        get;
        set
        {
            if (SetField(ref field, value))
            {
                UpdatePlacementCheck();
                Editor?.Redraw();
            }
        }
    }

    /// <summary>Prefab name, for messages.</summary>
    public string PrefabName { get; set; }

    public PlacePrefabTool(IEditorInstances instances) : base(Name, instances)
    {
    }

    public override void Reset()
    {
        _anchor = null;
        base.Reset();
    }

    public override void OnLayerChanged()
    {
        UpdatePlacementCheck();
    }

    public override void OnMouseMove(MouseInputInfo input)
    {
        base.OnMouseMove(input);

        _anchor = GetTileUnderMouse();
        Editor.Redraw();
    }

    public override void OnMouseLeave(MouseInputInfo input)
    {
        base.OnMouseLeave(input);
        _anchor = null;
        Editor.Redraw();
    }

    public override void OnButtonDown(MouseInputInfo input)
    {
        base.OnButtonDown(input);

        if (input.ActionButton != MouseButton.Left) return;

        _anchor = GetTileUnderMouse();

        if (_anchor.HasValue && Prefab is not null && _blockReason is null)
        {
            Place(Prefab, _anchor.Value);
        }

        Editor.Redraw();
    }

    private SKPointI? GetTileUnderMouse()
    {
        if (!MousePosition.HasValue) return null;

        var pos = Editor.ScreenToMap(MousePosition.Value);
        return new SKPointI((int)MathF.Floor(pos.X), (int)MathF.Floor(pos.Y));
    }

    private static bool IsMainLayer(string layerId) =>
        string.Equals(layerId, LayerDescription.MainLayerId, StringComparison.OrdinalIgnoreCase);

    private void UpdatePlacementCheck()
    {
        _blockReason = Prefab is null || Editor?.SelectedLayer is null
            ? "Pick a prefab in the Prefabs tab"
            : GetBlockReason(Prefab, Editor.SelectedLayer);
    }

    /// <summary>Why the prefab can't be placed with <paramref name="selectedLayer"/> selected; null when it can.</summary>
    private string GetBlockReason(PrefabFile prefab, MapLayer selectedLayer)
    {
        if (prefab.Layers.Count == 0)
            return "The prefab is empty";

        if (!prefab.IsLayerBound)
        {
            var part = prefab.Layers[0];

            if (IsMainLayer(part.Id) != IsMainLayer(selectedLayer.Id))
            {
                return IsMainLayer(part.Id)
                    ? "This prefab comes from the Main layer - select the Main layer"
                    : "This prefab comes from a background layer - select a layer other than Main";
            }

            return null;
        }

        var map = Instances.Map;

        var missing = prefab.Layers.Where(p => map.Layers.All(l => l.Id != p.Id)).Select(p => p.Name).ToArray();
        if (missing.Length > 0)
            return $"Layers missing in this map: {string.Join(", ", missing)}";

        if (prefab.Layers.All(p => p.Id != selectedLayer.Id))
            return $"Select one of the prefab's layers: {string.Join(", ", prefab.Layers.Select(p => p.Name))}";

        return null;
    }

    /// <summary>Target layer for each part: its own layer for a multi-layer prefab, the selected one otherwise.</summary>
    private IEnumerable<(MapLayer part, MapLayer target)> GetTargets(PrefabFile prefab)
    {
        var map = Instances.Map;

        foreach (var part in prefab.Layers)
        {
            var target = prefab.IsLayerBound
                ? map.Layers.FirstOrDefault(l => l.Id == part.Id)
                : Editor.SelectedLayer;

            if (target is not null)
            {
                yield return (part, target);
            }
        }
    }

    private void Place(PrefabFile prefab, SKPointI anchor)
    {
        var map = Instances.Map;
        var selectedLayer = Editor.SelectedLayer;

        Instances.UndoRedoServices.PushState();

        // New ids first, so links between the placed objects can be remapped.
        var nextId = map.NextObjectId;
        var newIds = new Dictionary<int, int>();

        foreach (var obj in prefab.Layers.SelectMany(l => l.Objects))
        {
            newIds[obj.Id] = nextId++;
        }

        foreach (var (part, target) in GetTargets(prefab))
        {
            // Layers with the same speed are aligned to their bottom edge (as in PrefabBuilder.Build).
            var x0 = anchor.X;
            var y0 = anchor.Y + target.Height - selectedLayer.Height;

            for (var x = 0; x < part.Width; ++x)
            {
                var tx = x0 + x;
                if (tx < 0 || tx >= target.Width) continue;

                for (var y = 0; y < part.Height; ++y)
                {
                    var ty = y0 + y;
                    if (ty < 0 || ty >= target.Height) continue;

                    var tile = part.Tiles[x, y];
                    if (tile.IsEmpty) continue;

                    target.Tiles[tx, ty] = tile;
                }
            }

            foreach (var obj in part.Objects)
            {
                var copy = obj.Clone(Instances.Template);

                copy.Id = newIds[obj.Id];
                copy.Position += new Vector2(x0, y0);
                RemapLinks(copy, newIds);

                target.Objects.Add(copy);
            }

            target.SortObjects();
        }

        map.OnModified();

        // Highlight what was placed.
        Editor.SetSelectedObjects(newIds.Values);
    }

    /// <summary>Points the object's link properties at the placed copies; links to objects outside the prefab are cleared.</summary>
    private static void RemapLinks(MapObject obj, IReadOnlyDictionary<int, int> newIds)
    {
        var parameters = obj.ParametersObject;
        if (parameters is null) return;

        var properties = parameters.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var property in properties)
        {
            if (property.PropertyType != typeof(int) || !property.CanWrite) continue;
            if (property.GetCustomAttribute<EditorLinkAttribute>() is null) continue;

            var linkedId = (int)property.GetValue(parameters);
            if (linkedId == 0) continue;

            property.SetValue(parameters, newIds.TryGetValue(linkedId, out var newId) ? newId : 0);
        }

        obj.UpdateLinks();
    }

    public override void Render(SKCanvas skCanvas, GRContext grContext, int width, int height)
    {
        base.Render(skCanvas, grContext, width, height);

        var prefab = Prefab;
        var anchor = _anchor;
        var blockReason = _blockReason;

        if (prefab is null || !anchor.HasValue) return;

        var ts = Instances.Template.TileSize * Editor.Zoom.Value;
        var origTs = Instances.Template.TileSize;

        var anchorMap = new Vector2(anchor.Value.X, anchor.Value.Y);
        var origin = Editor.MapToScreen(anchorMap);
        var alpha = (byte)(blockReason is null ? 200 : 90);

        // All parts sit on layers scrolling with the selected one, so they are drawn at the same anchor.
        var tilesets = Editor.GetTileSetImages(grContext);

        foreach (var part in prefab.Layers)
        {
            SkPaint.IsStroke = false;
            SkPaint.Color = SKColors.White.WithAlpha(alpha);

            for (var x = 0; x < part.Width; ++x)
            {
                for (var y = 0; y < part.Height; ++y)
                {
                    var tile = part.Tiles[x, y];
                    if (tile.IsEmpty || tile.TileSet >= tilesets.Count) continue;

                    var img = tilesets[tile.TileSet];
                    if (img is null) continue;

                    skCanvas.DrawImage(img,
                        SKRect.Create(tile.X * origTs, tile.Y * origTs, origTs, origTs),
                        SKRect.Create(origin.X + x * ts, origin.Y + y * ts, ts, ts),
                        SkPaint);
                }
            }

            foreach (var obj in part.Objects)
            {
                var image = GetEditorImage(obj);
                if (image is null) continue;

                SkPaint.IsStroke = false;
                SkPaint.Color = SKColors.White.WithAlpha(alpha);
                Editor.DrawEditorImage(image, obj.Flipped, anchorMap + obj.Position, skCanvas, grContext, SkPaint);
            }
        }

        var rect = SKRect.Create(origin.X, origin.Y, prefab.Width * ts, prefab.Height * ts);
        var color = blockReason is null ? ValidColor : InvalidColor;

        SkPaint.IsStroke = true;
        SkPaint.StrokeWidth = 2;
        SkPaint.Color = color.WithAlpha(200);
        skCanvas.DrawRect(rect, SkPaint);

        var label = blockReason ?? PrefabName;
        if (!string.IsNullOrEmpty(label))
        {
            var labelWidth = _font.MeasureText(label);
            var labelRect = SKRect.Create(rect.Left, rect.Top - _font.Size - 8, labelWidth + 8, _font.Size + 6);

            SkPaint.IsStroke = false;
            SkPaint.Color = SKColors.Black.WithAlpha(160);
            skCanvas.DrawRect(labelRect, SkPaint);

            SkPaint.Color = blockReason is null ? SKColors.White : SKColors.OrangeRed;
            skCanvas.DrawText(label, labelRect.Left + 4, labelRect.Bottom - 4, _font, SkPaint);
        }

        SkPaint.IsStroke = false;
        SkPaint.StrokeWidth = 1;
        SkPaint.Color = SKColors.White;
    }

    private EditorImage GetEditorImage(MapObject obj)
    {
        try
        {
            return obj.HasLogic
                ? Instances.ObjectsContainer.Get(obj.TypeId)
                : Instances.ImagesContainer.Get(obj.TypeId);
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }
}
