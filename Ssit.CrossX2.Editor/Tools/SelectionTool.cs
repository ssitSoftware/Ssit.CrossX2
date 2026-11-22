using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia.Input;
using SkiaSharp;
using Ssit.CrossX2.Editor.Helpers;
using Ssit.CrossX2.Editor.Input;
using Ssit.CrossX2.Editor.Models;
using Ssit.CrossX2.Editor.Service;
using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.Games.Map;

namespace Ssit.CrossX2.Editor.Tools
{
    /// <summary>
    /// Selects and moves objects, and selects regions.
    /// <list type="bullet">
    /// <item>Click an object to select it (repeated clicks cycle through overlapping objects), drag it to move it.
    /// Dragging any selected object moves the whole selection, objects on other layers included.</item>
    /// <item>Ctrl/Cmd + click adds objects to the selection (or removes them, if all objects under the cursor are
    /// already selected and the mouse is released without moving).</item>
    /// <item>Press on empty space and drag to select a region: its tiles (the source for Extract prefab) and the
    /// objects intersecting it - on the selected layer, and with <see cref="EditorData.SelectObjectsOnAllLayers"/>
    /// also on the other layers with the same speed. Moving the selection drops the region frame.</item>
    /// <item>Delete removes the selected objects, and with a region also clears its tiles on the selected layer.</item>
    /// </list>
    /// </summary>
    public class SelectionTool: EditorTool
    {
        public new const string Name = "Selection";

        // Screen distance the mouse has to move before a press on empty space becomes a region selection.
        private const float RegionDragThreshold = 4;

        private static readonly SKPathEffect DashEffect = SKPathEffect.CreateDash(new[] { 6f, 4f }, 0);
        private static readonly SKColor RegionColor = SKColors.DeepSkyBlue;

        private readonly EditorData _editorData;
        private readonly PrefabBuilder _prefabBuilder;
        private readonly SKFont _font = new() { Size = 12 };

        private readonly List<MapObjectInfo> _objectsBuffer = new();

        // Separate from _objectsBuffer, which Render fills on the render thread.
        private readonly List<MapObjectInfo> _clickBuffer = new();

        // Objects moved by the current drag, with their positions when the drag started.
        private readonly Dictionary<MapObject, Vector2> _draggedObjects = new();

        // Objects to remove from the selection on button up, if the mouse was not moved (Ctrl/Cmd + click).
        private readonly List<int> _objectsToDeselectAfter = new();

        private bool _initialMove;

        private Vector2 _initialMousePosition;
        private Vector2 _anchorInitialPosition;
        private MapObject _anchorObject;
        private int _objectToSelectAfter;

        private bool _canMoveObject;

        // Region drag: started by a press on empty space.
        private SKPointI? _regionStart;
        private Vector2 _regionStartScreen;
        private string _regionLayerId;
        private MapFile _regionMap;

        private Type _searchedType;
        private TaskCompletionSource<MapObject> _findObjectSource;
        private MapObject _matchingObject;

        /// <summary>Selected region, in tiles of <see cref="Layer"/>; null when no region is selected.</summary>
        public Rectangle? Region
        {
            get;
            private set => SetField(ref field, value);
        }

        /// <summary>Layer the region was selected on; null when no region is selected.</summary>
        public MapLayer Layer => Region.HasValue && _regionMap == Instances.Map
            ? Instances.Map?.Layers.FirstOrDefault(l => l.Id == _regionLayerId)
            : null;

        /// <summary>True when there is a region on the layer that is currently selected in the editor.</summary>
        public bool HasRegion => Layer is not null && Layer == Editor?.SelectedLayer;

        public SelectionTool(IEditorInstances instances, EditorData editorData) : base(Name, instances)
        {
            _editorData = editorData;
            _prefabBuilder = new PrefabBuilder(instances);
        }

        public Task<MapObject> SelectObject(Type type)
        {
            if (_findObjectSource != null)
            {
                _findObjectSource.TrySetCanceled();
            }
            _findObjectSource = new TaskCompletionSource<MapObject>();
            _searchedType = type;

            return _findObjectSource.Task;
        }

        public override void Reset()
        {
            base.Reset();

            _searchedType = null;
            _findObjectSource?.TrySetCanceled();
            _findObjectSource = null;
            _matchingObject = null;

            EndDrag();
            ClearRegion();
        }

        public override void OnLayerChanged()
        {
            // Undo / redo reloads the layers, so the region's layer is matched by id, not by instance.
            if (Region.HasValue && !HasRegion)
            {
                ClearRegion();
                Editor.SetSelectedObjects(Array.Empty<int>());
            }
        }

        /// <summary>Selects the region's objects again (e.g. after "select objects on all layers" changed).</summary>
        public void RefreshRegionSelection()
        {
            UpdateRegionSelection();
            Editor?.Redraw();
        }

        public override void OnHotkeyAction(HotkeyActions action)
        {
            if (action != HotkeyActions.Delete) return;

            var regionLayer = HasRegion ? Layer : null;

            if (regionLayer is null && Editor.SelectedObjects.Count == 0) return;

            Instances.UndoRedoServices.PushState();

            if (regionLayer is not null)
            {
                var region = Region.Value;

                var right = Math.Min(region.Right, regionLayer.Width);
                var bottom = Math.Min(region.Bottom, regionLayer.Height);

                for (var x = Math.Max(0, region.X); x < right; ++x)
                {
                    for (var y = Math.Max(0, region.Y); y < bottom; ++y)
                    {
                        regionLayer.Tiles[x, y] = Tile.Empty;
                    }
                }
            }

            foreach (var id in Editor.SelectedObjects.ToList())
            {
                Instances.Map.DeleteObject(id);
            }

            ClearRegion();
            Editor.SetSelectedObjects(Array.Empty<int>());
            Instances.Map.OnModified();
            Editor.Redraw();
        }

        public override void OnMouseMove(MouseInputInfo input)
        {
            base.OnMouseMove(input);

            if (!MousePosition.HasValue)
            {
                Editor.Redraw();
                return;
            }

            var leftButton = input.MouseButtons.HasFlag(MouseButton.Left);

            if (_regionStart.HasValue && leftButton)
            {
                DragRegion();
            }
            else if (_anchorObject != null && leftButton)
            {
                DragObjects(input);
            }
            else if (Region.HasValue && !input.MouseButtons.HasFlag(MouseButton.Right))
            {
                // Refreshes the selection after undo / redo restored or removed objects in the region.
                // Not while panning: the object bounds are from the last frame, before the view moved.
                UpdateRegionSelection();
            }

            Editor.Redraw();
        }

        private void DragRegion()
        {
            if (!Region.HasValue && (MousePosition.Value - _regionStartScreen).Length() < RegionDragThreshold)
                return;

            var tile = GetTileUnderMouse(clamp: true);
            if (!tile.HasValue) return;

            Region = FromCorners(_regionStart.Value, tile.Value);
            UpdateRegionSelection();
        }

        private void DragObjects(MouseInputInfo input)
        {
            var pos = Editor.ScreenToMap(MousePosition.Value);

            var offset = pos - _initialMousePosition;

            if (!_canMoveObject)
            {
                var orig = Editor.MapToScreen(_initialMousePosition);
                if ((orig - MousePosition.Value).Length() < 25)
                {
                    offset = Vector2.Zero;
                }
            }

            if (offset.LengthSquared() <= 0) return;

            _canMoveObject = true;
            if (_initialMove)
            {
                Instances.UndoRedoServices.PushState();
                _initialMove = false;
                _objectToSelectAfter = 0;
                _objectsToDeselectAfter.Clear();

                // The objects leave the region: keep them selected, drop the frame.
                ClearRegion();
            }

            var position = _anchorInitialPosition + offset;

            if (input.Modifiers.HasFlag(KeyModifiers.Control))
            {
                int x = (int) (position.X * 2);
                int y = (int) (position.Y * 2);

                position = new Vector2(x / 2f, y / 2f);
            }

            // The clicked object snaps, the rest of the selection keeps its distance to it. Layers with the same
            // speed share the tile grid, so objects on other layers move by the same amount.
            var delta = position - _anchorInitialPosition;

            foreach (var (obj, initialPosition) in _draggedObjects)
            {
                obj.Position = initialPosition + delta;
            }

            Instances.Map.OnModified();
        }

        public override void OnButtonUp(MouseInputInfo input)
        {
            base.OnButtonUp(input);

            if (_regionStart.HasValue && input.ActionButton == MouseButton.Left)
            {
                _regionStart = null;
                UpdateRegionSelection();
                Editor.Redraw();
                return;
            }

            if (_objectToSelectAfter != 0)
            {
                ClearRegion();
                Editor.SelectedObject = _objectToSelectAfter;
                Editor.Redraw();
            }
            else if (_objectsToDeselectAfter.Count > 0)
            {
                ClearRegion();
                Editor.SetSelectedObjects(Editor.SelectedObjects.Except(_objectsToDeselectAfter));
                Editor.Redraw();
            }

            EndDrag();
        }

        public override void OnButtonDown(MouseInputInfo input)
        {
            base.OnButtonDown(input);
            if (!MousePosition.HasValue) return;
            if (input.ActionButton != MouseButton.Left) return;

            if (_findObjectSource != null)
            {
                _findObjectSource.TrySetResult(_matchingObject);
                Reset();
                return;
            }

            Editor.GetMapObjects(_clickBuffer, MousePosition.Value);

            if (Platform.IsMultiSelect(input.Modifiers))
            {
                OnMultiSelectButtonDown();
                return;
            }

            if (_clickBuffer.Count == 0)
            {
                // Empty space: clear the selection; dragging from here selects a region.
                ClearRegion();
                Editor.SelectedObject = 0;

                _regionStart = GetTileUnderMouse(clamp: true);
                _regionStartScreen = MousePosition.Value;
                _regionLayerId = Editor.SelectedLayer?.Id;
                _regionMap = Instances.Map;
                return;
            }

            // Clicked an object of a multi-selection: drag the whole selection. Released without moving,
            // only the clicked object stays selected.
            if (Editor.SelectedObjects.Count > 1)
            {
                var member = _clickBuffer.FirstOrDefault(o => Editor.IsObjectSelected(o.Object.Id)).Object;
                if (member != null)
                {
                    BeginDrag(member);
                    _objectToSelectAfter = member.Id;
                    return;
                }
            }

            var index = _clickBuffer.FindIndex(o => o.Object.Id == Editor.SelectedObject) + 1;
            index %= _clickBuffer.Count;

            var obj = _clickBuffer[index].Object;

            if (Editor.SelectedObject == 0 || _clickBuffer.All(o => o.Object.Id != Editor.SelectedObject))
            {
                ClearRegion();
                Editor.SelectedObject = obj.Id;
                BeginDrag(obj);
            }
            else
            {
                // Drag the selected object; select the next overlapping one if released without moving.
                var selected = Instances.Map.FindObject(Editor.SelectedObject);
                BeginDrag(selected);
                _objectToSelectAfter = obj.Id;
            }
        }

        private void OnMultiSelectButtonDown()
        {
            // Ctrl/Cmd + click on empty space keeps the selection.
            if (_clickBuffer.Count == 0) return;

            var notSelected = _clickBuffer.FirstOrDefault(o => !Editor.IsObjectSelected(o.Object.Id)).Object;

            if (notSelected != null)
            {
                // A hand-picked selection: the region would otherwise select its own objects again.
                ClearRegion();
                Editor.AddToSelection(notSelected.Id);
                BeginDrag(notSelected);
                return;
            }

            // Everything under the cursor is selected already: dragging still moves the selection (so Ctrl
            // + drag snapping keeps working), a click without moving removes these objects from it.
            BeginDrag(_clickBuffer[0].Object);
            _objectsToDeselectAfter.AddRange(_clickBuffer.Select(o => o.Object.Id));
        }

        private void BeginDrag(MapObject anchor)
        {
            EndDrag();

            if (anchor is null) return;

            // FindObject searches all layers, so selected objects on other layers move along.
            foreach (var id in Editor.SelectedObjects)
            {
                var obj = Instances.Map.FindObject(id);
                if (obj != null)
                {
                    _draggedObjects[obj] = obj.Position;
                }
            }

            _draggedObjects[anchor] = anchor.Position;

            _initialMove = true;
            _initialMousePosition = Editor.ScreenToMap(MousePosition.Value);
            _anchorInitialPosition = anchor.Position;
            _anchorObject = anchor;
            _canMoveObject = false;

            Editor.Redraw();
        }

        private void EndDrag()
        {
            _anchorObject = null;
            _draggedObjects.Clear();
            _objectToSelectAfter = 0;
            _objectsToDeselectAfter.Clear();
            _regionStart = null;
        }

        /// <summary>
        /// Selects the objects intersecting the region: on the selected layer, and with "select objects on all
        /// layers" also on the other layers with the same speed (the objects Extract prefab takes from them).
        /// </summary>
        private void UpdateRegionSelection()
        {
            if (!HasRegion) return;

            var region = Region.Value;
            var selectedLayer = Editor.SelectedLayer;

            var topLeft = Editor.MapToScreen(new Vector2(region.X, region.Y));
            var bottomRight = Editor.MapToScreen(new Vector2(region.Right, region.Bottom));

            var screenRect = new RectangleF(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);

            Editor.GetMapObjects(_clickBuffer, screenRect);
            var ids = _clickBuffer.Select(o => o.Object.Id).ToList();

            if (_editorData.SelectObjectsOnAllLayers)
            {
                foreach (var layer in _prefabBuilder.GetMatchingLayers(selectedLayer))
                {
                    if (layer == selectedLayer) continue;

                    // Layers with the same speed are aligned to their bottom edge (see PrefabBuilder.Build).
                    var offsetY = layer.Height - selectedLayer.Height;
                    var area = new RectangleF(region.X, region.Y + offsetY, region.Width, region.Height);

                    foreach (var obj in layer.Objects)
                    {
                        var bounds = _prefabBuilder.GetObjectBounds(obj);
                        if (bounds.HasValue && bounds.Value.IsIntersecting(area))
                        {
                            ids.Add(obj.Id);
                        }
                    }
                }
            }

            Editor.SetSelectedObjects(ids);
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
            _regionLayerId = null;
            _regionMap = null;
        }

        public override void Render(SKCanvas skCanvas, GRContext grContext, int width, int height)
        {
            var template = Instances.Template;

            base.Render(skCanvas, grContext, width, height);
            var ts = template.TileSize * Editor.Zoom.Value;

            RenderOtherLayersSelection(skCanvas, ts);

            if (HasRegion)
            {
                RenderRegion(skCanvas, Region.Value, ts);
            }

            RenderHover(skCanvas, ts);

            SkPaint.PathEffect = null;
            SkPaint.IsStroke = false;
            SkPaint.StrokeWidth = 1;
            SkPaint.Color = SKColors.White;
        }

        private void RenderHover(SKCanvas skCanvas, float ts)
        {
            if (!MousePosition.HasValue) return;

            Editor.GetMapObjects(_objectsBuffer, MousePosition.Value);

            for (var idx = 0; idx < _objectsBuffer.Count; ++idx)
            {
                if (_searchedType != null && _searchedType.IsAssignableFrom(_objectsBuffer[idx].Object.Type))
                {
                    SkPaint.IsStroke = true;
                    SkPaint.StrokeWidth = 2;
                    SkPaint.Color = SKColors.YellowGreen.WithAlpha(192);
                    skCanvas.DrawRect(_objectsBuffer[idx].ScreenBounds.ToSkia(), SkPaint);
                    skCanvas.DrawOval(_objectsBuffer[idx].ScreenBounds.Inflate(10).ToSkia(), SkPaint);
                    _matchingObject = _objectsBuffer[idx].Object;
                    return;
                }

                SkPaint.IsStroke = true;
                SkPaint.StrokeWidth = 2;
                SkPaint.Color = SKColors.HotPink.WithAlpha(96);
                skCanvas.DrawRect(_objectsBuffer[idx].ScreenBounds.ToSkia(), SkPaint);
            }

            SkPaint.StrokeWidth = 1;
            SkPaint.Color = SKColors.White;

            if (_objectsBuffer.Count > 0 || _regionStart.HasValue)
                return;

            var pos = Editor.ScreenToMap(MousePosition.Value);

            if (pos.X >= 0 && pos.Y >= 0 && pos.X < Editor.SelectedLayer.Width && pos.Y < Editor.SelectedLayer.Height)
            {
                pos.X = (int) pos.X;
                pos.Y = (int) pos.Y;

                pos = Editor.MapToScreen(pos);

                SkPaint.IsStroke = false;
                SkPaint.Color = SKColors.Pink.WithAlpha(32);

                skCanvas.DrawRect(pos.X, pos.Y, ts, ts, SkPaint);
            }
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

        /// <summary>
        /// Dashed outlines of selected objects on the other layers with the same speed - those layers are usually
        /// not drawn, so their objects' own selection highlight isn't visible.
        /// </summary>
        private void RenderOtherLayersSelection(SKCanvas skCanvas, float ts)
        {
            var selectedLayer = Editor.SelectedLayer;
            if (selectedLayer is null || Editor.SelectedObjects.Count == 0) return;

            SkPaint.IsStroke = true;
            SkPaint.StrokeWidth = 1;
            SkPaint.Color = RegionColor.WithAlpha(200);
            SkPaint.PathEffect = DashEffect;

            foreach (var layer in _prefabBuilder.GetMatchingLayers(selectedLayer))
            {
                if (layer == selectedLayer) continue;

                var offsetY = layer.Height - selectedLayer.Height;

                for (var idx = 0; idx < layer.Objects.Count; ++idx)
                {
                    var obj = layer.Objects[idx];
                    if (!Editor.IsObjectSelected(obj.Id)) continue;

                    var bounds = _prefabBuilder.GetObjectBounds(obj);
                    if (!bounds.HasValue) continue;

                    var pos = Editor.MapToScreen(new Vector2(bounds.Value.X, bounds.Value.Y - offsetY));
                    skCanvas.DrawRect(SKRect.Create(pos.X, pos.Y, bounds.Value.Width * ts, bounds.Value.Height * ts),
                        SkPaint);
                }
            }

            SkPaint.PathEffect = null;
        }
    }
}
