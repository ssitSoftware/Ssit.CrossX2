using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia.Input;
using SkiaSharp;
using Ssit.CrossX2.Editor.Helpers;
using Ssit.CrossX2.Editor.Input;
using Ssit.CrossX2.Editor.Service;
using Ssit.CrossX2.Framework.Games.Map;

namespace Ssit.CrossX2.Editor.Tools
{
    /// <summary>
    /// Selects and moves objects. Click selects one object (repeated clicks cycle through overlapping objects).
    /// Ctrl/Cmd + click adds objects to the selection (or removes them, if all objects under the cursor are
    /// already selected and the mouse is released without moving). Dragging moves all selected objects together,
    /// Delete removes all of them.
    /// </summary>
    public class SelectionTool: EditorTool
    {
        public new const string Name = "Selection";

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

        private Type _searchedType;
        private TaskCompletionSource<MapObject> _findObjectSource;
        private MapObject _matchingObject;

        public SelectionTool(IEditorInstances instances) : base(Name, instances)
        {
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
        }

        public override void OnHotkeyAction(HotkeyActions action)
        {
            switch (action)
            {
                case HotkeyActions.Delete:
                    if (Editor.SelectedObjects.Count > 0)
                    {
                        Instances.UndoRedoServices.PushState();

                        foreach (var id in Editor.SelectedObjects.ToList())
                        {
                            Instances.Map.DeleteObject(id);
                        }

                        Editor.SetSelectedObjects(Array.Empty<int>());
                        Editor.Redraw();
                    }
                    break;
            }
        }

        public override void OnMouseMove(MouseInputInfo input)
        {
            base.OnMouseMove(input);

            if (_anchorObject != null && input.MouseButtons.HasFlag(MouseButton.Left))
            {
                if (!MousePosition.HasValue) return;

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

                if (offset.LengthSquared() > 0)
                {
                    _canMoveObject = true;
                    if (_initialMove)
                    {
                        Instances.UndoRedoServices.PushState();
                        _initialMove = false;
                        _objectToSelectAfter = 0;
                        _objectsToDeselectAfter.Clear();
                    }

                    var position = _anchorInitialPosition + offset;

                    if (input.Modifiers.HasFlag(KeyModifiers.Control))
                    {
                        int x = (int) (position.X * 2);
                        int y = (int) (position.Y * 2);

                        position = new Vector2(x / 2f, y / 2f);
                    }

                    // The clicked object snaps, the rest of the selection keeps its distance to it.
                    var delta = position - _anchorInitialPosition;

                    foreach (var (obj, initialPosition) in _draggedObjects)
                    {
                        obj.Position = initialPosition + delta;
                    }

                    Instances.Map.OnModified();
                }
            }

            Editor.Redraw();
        }

        public override void OnButtonUp(MouseInputInfo input)
        {
            base.OnButtonUp(input);

            if (_objectToSelectAfter != 0)
            {
                Editor.SelectedObject = _objectToSelectAfter;
                Editor.Redraw();
            }
            else if (_objectsToDeselectAfter.Count > 0)
            {
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
                Editor.SelectedObject = 0;
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
        }

        public override void Render(SKCanvas skCanvas, GRContext grContext, int width, int height)
        {
            var template = Instances.Template;

            base.Render(skCanvas, grContext, width, height);
            var ts = template.TileSize * Editor.Zoom.Value;

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

            if (_objectsBuffer.Count > 0)
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
    }
}
