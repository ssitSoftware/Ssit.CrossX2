using Avalonia.Input;
using SkiaSharp;
using Ssit.CrossX2.Editor.Helpers;
using Ssit.CrossX2.Editor.Input;
using Ssit.CrossX2.Editor.Service;
using Ssit.CrossX2.Framework.Games.Map;

namespace Ssit.CrossX2.Editor.Tools;

public class SetMaterialTool(IEditorInstances instances) : EditorTool(Name, instances)
{
    public new const string Name = "Set Material";
    
    public int MaterialIndex { get; set; }

    private bool _pushState;

    public override void OnButtonDown(MouseInputInfo input)
    {
        base.OnButtonDown(input);

        Editor.ShowMaterials = true;
        
        if (input.ActionButton == MouseButton.Left)
        {
            _pushState = true;
            SetMaterial();
            Editor.Redraw();
        }
    }

    private void SetMaterial()
    {
        if (!MousePosition.HasValue) return;
        var pos = Editor.ScreenToMap(MousePosition.Value);

        var ix = (int) pos.X;
        var iy = (int) pos.Y;
        
        var layer = Editor.SelectedLayer;
        if (layer != Instances.Map.MainLayer)
            return;
        
        if (ix < 0) return;
        if (iy < 0) return;
        
        if (ix >= layer.Width) return;
        if (iy >= layer.Height) return;

        var tile = layer.Tiles[ix, iy];
        
        if (tile.IsEmpty || tile.Material == MaterialIndex)
            return;
        
        if (_pushState)
        {
            _pushState = false;
            Instances.UndoRedoServices.PushState();
        }
        
        layer.Tiles[ix, iy] = new Tile(tile.TileSet, tile.X, tile.Y, MaterialIndex);
        
        Instances.Map.OnModified();
    }

    public override void OnMouseMove(MouseInputInfo input)
    {
        base.OnMouseMove(input);

        if (input.MouseButtons == MouseButton.Left)
        {
            SetMaterial();
        }
        
        Editor.Redraw();
    }
    
    public override void Render(SKCanvas skCanvas, GRContext grContext, int width, int height)
    {
        var template = Instances.Template;
        
        base.Render(skCanvas, grContext, width, height);
        var ts = template.TileSize * Editor.Zoom.Value;

        if (!MousePosition.HasValue) return;
            
        var pos = Editor.ScreenToMap(MousePosition.Value);

        if (pos.X >= 0 && pos.Y >= 0 && pos.X < Editor.SelectedLayer.Width && pos.Y < Editor.SelectedLayer.Height)
        {
            pos.X = (int) pos.X;
            pos.Y = (int) pos.Y;

            pos = Editor.MapToScreen(pos);

            SkPaint.IsStroke = false;
            SkPaint.Color = (Instances.Template.Materials[MaterialIndex].PreviewColor * 0.25f).ToSkia();

            skCanvas.DrawRect(pos.X, pos.Y, ts, ts, SkPaint);
        }
    }
}