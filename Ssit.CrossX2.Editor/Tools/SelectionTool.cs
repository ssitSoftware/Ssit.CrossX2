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
    public class SelectionTool: EditorTool
    {
        public new const string Name = "Selection";

        private readonly List<MapObjectInfo> _objectsBuffer = new();

        private bool _initialMove;

        private Vector2 _initialMousePosition; 
        private Vector2 _initialObjectPosition;
        private MapObject _draggedObject;
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
        }

        public override void OnHotkeyAction(HotkeyActions action)
        {
            switch (action)
            {
                case HotkeyActions.Delete:
                    if (Editor.SelectedObject != 0)
                    {
                        Instances.UndoRedoServices.PushState();
                        Instances.Map.DeleteObject(Editor.SelectedObject);
                        Editor.SelectedObject = 0;
                        Editor.Redraw();
                    }
                    break;
            }
        }

        public override void OnMouseMove(MouseInputInfo input)
        {
            base.OnMouseMove(input);

            if (_draggedObject != null && input.MouseButtons.HasFlag(MouseButton.Left))
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
                    }

                    var position = _initialObjectPosition + offset;
                    
                    if (input.Modifiers.HasFlag(KeyModifiers.Control))
                    {
                        int x = (int) (position.X * 2);
                        int y = (int) (position.Y * 2);

                        position = new Vector2(x / 2f, y / 2f);
                    }

                    _draggedObject.Position = position;
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
            
            _draggedObject = null;
            _objectToSelectAfter = 0;
        }

        public override void OnButtonDown(MouseInputInfo input)
        {
            base.OnButtonDown(input);
            if (!MousePosition.HasValue) return;
            
            if (input.ActionButton == MouseButton.Left)
            {
                if (_findObjectSource != null)
                {
                    _findObjectSource.TrySetResult(_matchingObject);
                    Reset();
                    return;
                }
                
                Editor.GetMapObjects(_objectsBuffer, MousePosition.Value);

                var index = _objectsBuffer.FindIndex(o => o.Object.Id == Editor.SelectedObject) + 1;
                index %= Math.Max(1, _objectsBuffer.Count);
                
                if (index < _objectsBuffer.Count)
                {
                    var obj = _objectsBuffer[index].Object;

                    if (Editor.SelectedObject == 0 || null == _objectsBuffer.FirstOrDefault( o=>o.Object.Id == Editor.SelectedObject).Object)
                    {
                        Editor.SelectedObject = obj.Id;
                    }
                    else
                    {
                        _objectToSelectAfter = obj.Id;
                        obj = Instances.Map.FindObject(Editor.SelectedObject);
                    }
                    
                    _initialMove = true;
                    _initialMousePosition = Editor.ScreenToMap(MousePosition.Value);
                    _initialObjectPosition = obj.Position;
                    _draggedObject = obj;
                    _canMoveObject = false;
                    
                    Editor.Redraw();
                }
                else
                {
                    Editor.SelectedObject = 0;
                }
            }
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