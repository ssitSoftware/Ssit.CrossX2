using System;
using System.Numerics;
using Avalonia.Input;
using SkiaSharp;
using Ssit.CrossX2.Editor.Input;
using Ssit.CrossX2.Editor.Service;
using Ssit.CrossX2.Framework.Games.Map;

namespace Ssit.CrossX2.Editor.Tools
{
    public class EraserTool: EditorTool
    {
        public new const string Name = "Eraser";
    
        public EraserTool(IEditorInstances instances) : base(Name, instances)
        {
        }

        public int Size
        {
            get;
            set => SetField(ref field, value);
        } = 1;

        public override void OnMouseMove(MouseInputInfo input)
        {
            base.OnMouseMove(input);
        
            if (input.MouseButtons == MouseButton.Left)
            {
                EraseTiles();
            }
        
            Editor.Redraw();
        }
    
        public override bool OnMouseWheel(MouseInputInfo input)
        {
            if (input.Modifiers == KeyModifiers.Control)
            {
                var size = Size + Math.Sign(input.Delta.Y);

                Size = Math.Max(1, Math.Min(16, size));
                Editor.Redraw();
                return true;
            }
        
            return base.OnMouseWheel(input);
        }
    
        public override void OnButtonDown(MouseInputInfo input)
        {
            base.OnButtonDown(input);

            if (input.ActionButton == MouseButton.Left)
            {
                EraseTiles();
                Editor.Redraw();
            }
        }

        private void EraseTiles()
        {
            var position = GetTargetPosition();
        
            if (!position.HasValue) return;
            var pos = position.Value;

            var layer = Editor.SelectedLayer;
        
            for (var x = 0; x < Size; ++x)
            {
                var xx = pos.X + x;

                if (xx < 0 || xx >= layer.Width) continue;
            
                for (var y = 0; y < Size; ++y)
                {
                    var yy = pos.Y + y;
                    if (yy < 0 || yy >= layer.Height) continue;

                    layer.Tiles[xx, yy] = Tile.Empty;
                }
            }

            Instances.Map.OnModified();
        }

        private SKPointI? GetTargetPosition()
        {
            if (!MousePosition.HasValue) return null;
        
            var pos = Editor.ScreenToMap(MousePosition.Value);

            pos.X -= (Size-1) / 2f;
            pos.Y -= (Size-1) / 2f;
        
            return new SKPointI((int)pos.X, (int)pos.Y);
        }

        public override void Render(SKCanvas skCanvas, GRContext grContext, int width, int height)
        {
            var template = Instances.Template;
        
            base.Render(skCanvas, grContext, width, height);
            var ts = template.TileSize * Editor.Zoom.Value;

            var position = GetTargetPosition();
        
            if (!position.HasValue) return;
            var pos = new Vector2(position.Value.X, position.Value.Y);
        
            pos = Editor.MapToScreen(pos);

            SkPaint.IsStroke = false;
            SkPaint.Color = SKColors.Red.WithAlpha(64);
        
            skCanvas.DrawRect(pos.X, pos.Y, ts * Size, ts * Size, SkPaint);
        }
    }
}