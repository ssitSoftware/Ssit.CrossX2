using System.Numerics;
using Avalonia.Input;
using SkiaSharp;
using Ssit.CrossX2.Editor.Helpers;
using Ssit.CrossX2.Editor.Input;
using Ssit.CrossX2.Editor.Service;
using Ssit.CrossX2.Framework.Utils;

namespace Ssit.CrossX2.Editor.Tools
{
    public abstract class EditorTool(string name, IEditorInstances instances) : BindableModel, IPointerHandler
    {
        public string Name { get; } = name;
        protected IEditorInstances Instances { get; } = instances;

        protected readonly SKPaint SkPaint = new();

        private Vector2? _rightPos;
        private Vector2? _position;

        protected Vector2? MousePosition => _position;

        protected IEditor Editor => Instances.Editor;

        public virtual void OnFinished()
        {
            Reset();
        }
        
        public virtual void OnHotkeyAction(HotkeyActions action)
        {
            
        }
        
        public virtual void Render(SKCanvas skCanvas, GRContext grContext, int width, int height)
        {
        }

        public virtual void OnMouseMove(MouseInputInfo input)
        {
            _position = input.Position;
            if (_rightPos.HasValue && input.MouseButtons.HasFlag(MouseButton.Right))
            {
                var pos = input.Position;

                var offset = Editor.ScreenToMap(pos) - Editor.ScreenToMap(_rightPos.Value);
                _rightPos = pos;

                Editor.Offset -= offset;
            }
        }

        public virtual void OnButtonDown(MouseInputInfo input)
        {
            if (input.ActionButton == MouseButton.Right)
            {
                _rightPos = input.Position;
            }
        }

        public virtual void OnButtonUp(MouseInputInfo input)
        {
            if (input.ActionButton == MouseButton.Right)
            {
                _rightPos = null;
            }
        }

        public virtual void OnMouseLeave(MouseInputInfo input)
        {
            _position = null;
        }

        public virtual void Reset()
        {
            _position = null;
            Editor?.Redraw();
        }

        public virtual bool OnMouseWheel(MouseInputInfo input)
        {
            if (input.Modifiers == KeyModifiers.Control)
            {
                var mapPos = Editor.ScreenToMap(input.Position);
            
                if (input.Delta.Y > 0)
                {
                    if (Editor.Zoom.ZoomInCommand.CanExecute(null))
                    {
                        Editor.Zoom.ZoomInCommand.Execute(null);
                    }
                }
                else
                {
                    if (Editor.Zoom.ZoomOutCommand.CanExecute(null))
                    {
                        Editor.Zoom.ZoomOutCommand.Execute(null);
                    }
                }

                var mapPos2 = Editor.ScreenToMap(input.Position);

                Editor.Offset += mapPos - mapPos2;
                Editor.EnsurePanInBounds();
                Editor.Redraw();
                return true;
            }
        

            var offset = Vector2.Zero;

            offset.X +=  input.Delta.X * 4 / Editor.Zoom.Value;
            offset.Y += input.Delta.Y * 4 / Editor.Zoom.Value;

            Editor.Offset -= offset;
            Editor.EnsurePanInBounds();
            Editor.Redraw();
            return true;
        }
    }
}