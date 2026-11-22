using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Avalonia.Input;
using SkiaSharp;
using Ssit.CrossX2.Editor.Helpers;
using Ssit.CrossX2.Editor.Input;
using Ssit.CrossX2.Editor.Service;
using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.Games.Map;

namespace Ssit.CrossX2.Editor.Tools;

/// <summary>
/// Selects a rectangular region of tiles on the selected layer together with the objects of that layer
/// that intersect it (they become the editor's object selection). This is the source for extracting a prefab.
/// Drag with the left button to select, Delete clears the tiles of the region and deletes the selected objects,
/// Escape (or changing the layer) drops the selection.
/// </summary>
public class RegionSelectionTool : EditorTool
{
    public new const string Name = "Region Selection";

    private static readonly SKPathEffect DashEffect = SKPathEffect.CreateDash(new[] { 6f, 4f }, 0);

    private static readonly SKColor RegionColor = SKColors.DeepSkyBlue;

    private readonly List<MapObjectInfo> _objectsBuffer = new();
    private readonly SKFont _font = new() { Size = 12 };

    private SKPointI? _dragStart;
    private string _layerId;
    private MapFile _regionMap;

    /// <summary>Selected region, in tiles of <see cref="Layer"/>; null when nothing is selected.</summary>
    public Rectangle? Region
    {
        get;
        private set => SetField(ref field, value);
    }

    /// <summary>Layer the region was selected on; null when nothing is selected.</summary>
    public MapLayer Layer => Region.HasValue && _regionMap == Instances.Map
        ? Instances.Map?.Layers.FirstOrDefault(l => l.Id == _layerId)
        : null;

    /// <summary>True when there is a region on the layer that is currently selected in the editor.</summary>
    public bool HasSelection => Layer is not null && Layer == Editor?.SelectedLayer;

    public RegionSelectionTool(IEditorInstances instances) : base(Name, instances)
    {
    }

    public override void Reset()
    {
        ClearRegion();
        base.Reset();
    }

    public override void OnLayerChanged()
    {
        // Undo / redo reloads the layers, so the layer is matched by id, not by instance.
        if (Region.HasValue && !HasSelection)
        {
            ClearRegion();
            Editor.SetSelectedObjects(Array.Empty<int>());
        }
    }

    public override void OnHotkeyAction(HotkeyActions action)
    {
        if (action != HotkeyActions.Delete) return;
        if (!HasSelection) return;

        var layer = Layer;
        var region = Region.Value;

        Instances.UndoRedoServices.PushState();

        var right = Math.Min(region.Right, layer.Width);
        var bottom = Math.Min(region.Bottom, layer.Height);

        for (var x = Math.Max(0, region.X); x < right; ++x)
        {
            for (var y = Math.Max(0, region.Y); y < bottom; ++y)
            {
                layer.Tiles[x, y] = Tile.Empty;
            }
        }

        foreach (var id in Editor.SelectedObjects.ToList())
        {
            Instances.Map.DeleteObject(id);
        }

        Editor.SetSelectedObjects(Array.Empty<int>());
        Instances.Map.OnModified();
        Editor.Redraw();
    }

    public override void OnButtonDown(MouseInputInfo input)
    {
        base.OnButtonDown(input);
        if (input.ActionButton != MouseButton.Left) return;

        var tile = GetTileUnderMouse(clamp: true);
        if (!tile.HasValue) return;

        _dragStart = tile;
        _layerId = Editor.SelectedLayer.Id;
        _regionMap = Instances.Map;
        Region = FromCorners(tile.Value, tile.Value);

        UpdateSelectedObjects();
        Editor.Redraw();
    }

    public override void OnMouseMove(MouseInputInfo input)
    {
        base.OnMouseMove(input);

        if (_dragStart.HasValue && input.MouseButtons.HasFlag(MouseButton.Left))
        {
            var tile = GetTileUnderMouse(clamp: true);
            if (tile.HasValue)
            {
                Region = FromCorners(_dragStart.Value, tile.Value);
            }
        }

        // Also refreshes the selection after undo / redo restored or removed objects in the region.
        // Not while panning: the object bounds are from the last frame, before the view moved.
        if (!input.MouseButtons.HasFlag(MouseButton.Right))
        {
            UpdateSelectedObjects();
        }

        Editor.Redraw();
    }

    public override void OnButtonUp(MouseInputInfo input)
    {
        base.OnButtonUp(input);
        if (input.ActionButton != MouseButton.Left) return;

        _dragStart = null;
        UpdateSelectedObjects();
    }

    public override void Render(SKCanvas skCanvas, GRContext grContext, int width, int height)
    {
        base.Render(skCanvas, grContext, width, height);

        var ts = Instances.Template.TileSize * Editor.Zoom.Value;

        if (!_dragStart.HasValue)
        {
            RenderHoveredTile(skCanvas, ts);
        }

        if (HasSelection)
        {
            RenderRegion(skCanvas, Region.Value, ts);
        }

        SkPaint.PathEffect = null;
        SkPaint.IsStroke = false;
        SkPaint.StrokeWidth = 1;
        SkPaint.Color = SKColors.White;
    }

    private void RenderRegion(SKCanvas skCanvas, Rectangle region, float ts)
    {
        var pos = Editor.MapToScreen(new Vector2(region.X, region.Y));
        var rect = SKRect.Create(pos.X, pos.Y, region.Width * ts, region.Height * ts);

        SkPaint.IsStroke = false;
        SkPaint.Color = RegionColor.WithAlpha(40);
        skCanvas.DrawRect(rect, SkPaint);

        SkPaint.IsStroke = true;
        SkPaint.StrokeWidth = 2;
        SkPaint.Color = RegionColor.WithAlpha(220);
        SkPaint.PathEffect = DashEffect;
        skCanvas.DrawRect(rect, SkPaint);
        SkPaint.PathEffect = null;

        var objects = Editor.SelectedObjects.Count;
        var label = string.Create(CultureInfo.InvariantCulture,
            $"{region.Width} x {region.Height} tiles, {objects} object{(objects == 1 ? "" : "s")}");

        var labelWidth = _font.MeasureText(label);
        var labelRect = SKRect.Create(rect.Left, rect.Top - _font.Size - 8, labelWidth + 8, _font.Size + 6);

        SkPaint.IsStroke = false;
        SkPaint.Color = SKColors.Black.WithAlpha(160);
        skCanvas.DrawRect(labelRect, SkPaint);

        SkPaint.Color = SKColors.White;
        skCanvas.DrawText(label, labelRect.Left + 4, labelRect.Bottom - 4, _font, SkPaint);
    }

    private void RenderHoveredTile(SKCanvas skCanvas, float ts)
    {
        var tile = GetTileUnderMouse(clamp: false);
        if (!tile.HasValue) return;

        var pos = Editor.MapToScreen(new Vector2(tile.Value.X, tile.Value.Y));

        SkPaint.IsStroke = false;
        SkPaint.Color = RegionColor.WithAlpha(48);
        skCanvas.DrawRect(pos.X, pos.Y, ts, ts, SkPaint);
    }

    /// <summary>Selects the objects of the selected layer that intersect the region.</summary>
    private void UpdateSelectedObjects()
    {
        if (!HasSelection) return;

        var region = Region.Value;

        var topLeft = Editor.MapToScreen(new Vector2(region.X, region.Y));
        var bottomRight = Editor.MapToScreen(new Vector2(region.Right, region.Bottom));

        var screenRect = new RectangleF(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);

        Editor.GetMapObjects(_objectsBuffer, screenRect);
        Editor.SetSelectedObjects(_objectsBuffer.Select(o => o.Object.Id).ToList());
    }

    private SKPointI? GetTileUnderMouse(bool clamp)
    {
        if (!MousePosition.HasValue) return null;

        var layer = Editor.SelectedLayer;
        if (layer is null || layer.Width == 0 || layer.Height == 0) return null;

        var pos = Editor.ScreenToMap(MousePosition.Value);

        var x = (int)MathF.Floor(pos.X);
        var y = (int)MathF.Floor(pos.Y);

        if (clamp)
        {
            return new SKPointI(Math.Clamp(x, 0, layer.Width - 1), Math.Clamp(y, 0, layer.Height - 1));
        }

        if (x < 0 || y < 0 || x >= layer.Width || y >= layer.Height) return null;

        return new SKPointI(x, y);
    }

    private static Rectangle FromCorners(SKPointI a, SKPointI b)
    {
        var x = Math.Min(a.X, b.X);
        var y = Math.Min(a.Y, b.Y);

        return new Rectangle(x, y, Math.Abs(a.X - b.X) + 1, Math.Abs(a.Y - b.Y) + 1);
    }

    private void ClearRegion()
    {
        Region = null;
        _dragStart = null;
        _layerId = null;
        _regionMap = null;
    }
}
