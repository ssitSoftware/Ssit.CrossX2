using System;
using System.Numerics;
using Avalonia.Input;
using SkiaSharp;
using Ssit.CrossX2.Editor.Helpers;
using Ssit.CrossX2.Editor.Input;
using Ssit.CrossX2.Editor.Service;
using Ssit.CrossX2.Editor.Tools;
using Ssit.CrossX2.Framework.Games.Map;
using Ssit.CrossX2.Framework.Utils;

namespace Ssit.CrossX2.Editor.ViewModels
{
    public class TilesetSelectorViewModel: BindableModel, ISkRenderer, IPointerHandler
    {
        private readonly IEditorInstances _instances;
        private Tileset _selectedTileset;

        public Tileset SelectedTileset
        {
            get => _selectedTileset;
            set
            {
                if (SetField(ref _selectedTileset, value))
                {
                    UpdateSize();
                }
            }
        }

        public string SelectedTiles
        {
            get
            {
                if (!_selectionStart.HasValue)
                    return "";
                
                if (!_selectionEnd.HasValue)
                    return "";

                var rect = CalculateRect(_selectionStart.Value, _selectionEnd.Value);

                return $"({rect.Left}, {rect.Top}, {rect.Width}, {rect.Height})";
            }
        }

        public bool ShowGrid
        {
            get;
            set
            {
                if (SetField(ref field, value))
                {
                    RedrawNeeded?.Invoke();
                }
            }
        }


        public event Action RedrawNeeded;

        private SKPointI? _selectionStart;
        private SKPointI? _selectionEnd;

        private SKPaint _skPaint = new ();

        public Vector2 Size
        {
            get;
            set => SetField(ref field, value);
        }

        public ZoomViewModel Zoom { get; }

        public TilesetSelectorViewModel(IEditorInstances instances)
        {
            _instances = instances;
            Zoom = new ZoomViewModel(UpdateSize);
            ShowGrid = true;
            SelectedTileset = _instances.TilesetsContainer.TileSets[0];
        }

        private void UpdateSize()
        {
            var img = _selectedTileset?.Get(null);

            var size = new Vector2(100,100);

            if (img is not null)
            {
                size.X = img.Width * Zoom.Value + 1;
                size.Y = img.Height * Zoom.Value + 1;
            }
        
            Size = size;
            RedrawNeeded?.Invoke();
        }
    
        public void Render(SKCanvas skCanvas, GRContext grContext, int width, int height)
        {
            var template = _instances.Template;
            var tools = _instances.Tools;
            var tileSets = _instances.TilesetsContainer;

            var img = SelectedTileset?.Get(grContext);

            if (img is null) return;

            var imgWidth = Zoom.Value * img.Width;
            var imgHeight = Zoom.Value * img.Height;
        
            skCanvas.DrawImage(img, new SKRect(0, 0, imgWidth, imgHeight));

            var ts = template.TileSize * Zoom.Value;
            var insertTilesTool = tools.GetTool<InsertTilesTool>();

            _skPaint.Color = SKColors.YellowGreen.WithAlpha(128);
            _skPaint.IsStroke = false;
            var tileIndex = Array.IndexOf(tileSets.TileSets, SelectedTileset);
        
            foreach (var ut in insertTilesTool.UniqueTiles)
            {
                var tile = new Tile(ut);
                if (tile.TileSet == tileIndex)
                {
                    skCanvas.DrawRect(new SKRect(tile.X * ts, tile.Y * ts, tile.X * ts + ts, tile.Y * ts + ts), _skPaint);
                }
            }

            SKRect? selectionRect = null;

            if (_selectionStart.HasValue && _selectionEnd.HasValue)
            {
                selectionRect = CalculateRect(_selectionStart.Value, _selectionEnd.Value);
                _skPaint.Color = SKColors.Gold.WithAlpha(128);
            }

            if (selectionRect.HasValue)
            {
                var rect = selectionRect.Value;

                _skPaint.IsStroke = false;
            
                skCanvas.DrawRect( new SKRect(rect.Left * ts, rect.Top * ts, rect.Right * ts, rect.Bottom * ts), _skPaint );
            }
        
            if (ShowGrid)
            {
                _skPaint.IsStroke = true;
                _skPaint.StrokeWidth = 1;
                _skPaint.IsAntialias = false;
                _skPaint.Color = SKColors.White.WithAlpha(32);

                for (var idx = 0; idx < (int) (imgWidth / ts) + 1; ++idx)
                {
                    skCanvas.DrawLine(idx * ts + 0.5f, 0, idx* ts, imgHeight, _skPaint);
                }
            
                for (var idx = 0; idx < (int) (imgHeight / ts) + 1; ++idx)
                {
                    skCanvas.DrawLine(0, idx * ts, imgWidth, idx* ts, _skPaint);
                }
            }
        }

        public void UnloadResources()
        {
        
        }

        public void OnMouseMove(MouseInputInfo input)
        {
            if (_selectionStart.HasValue)
            {
                _selectionEnd = GetIntPosition(input.Position);
                OnPropertyChanged(nameof(SelectedTiles));
                RedrawNeeded?.Invoke();
            }
        }

        public void OnButtonDown(MouseInputInfo input)
        {
            if (input.ActionButton == MouseButton.Left)
            {
                _selectionStart = GetIntPosition(input.Position);
                _selectionEnd = _selectionStart;
                OnPropertyChanged(nameof(SelectedTiles));
                RedrawNeeded?.Invoke();
            }
        }

        private SKPointI GetIntPosition(Vector2 pos)
        {
            var template = _instances.Template;
            var ts = template.TileSize * Zoom.Value;
            var newPos = pos / ts;
            return new SKPointI((int)newPos.X, (int)newPos.Y);
        }

        public void OnButtonUp(MouseInputInfo input)
        {
            if (input.ActionButton == MouseButton.Left && _selectionStart.HasValue && _selectionEnd.HasValue)
            {
                var tools = _instances.Tools;
                var selection = CalculateRect(_selectionStart.Value, _selectionEnd.Value);
                
                _selectionStart = null;
                _selectionEnd = null;
                RedrawNeeded?.Invoke();

                var tool = tools.GetTool<InsertTilesTool>();
                tool.SetTiles(SelectedTileset, selection);
                _instances.Tools.Current = tool;
                
                OnPropertyChanged(nameof(SelectedTiles));
            }
        }

        public void OnMouseLeave(MouseInputInfo input)
        {
        
        }

        public bool OnMouseWheel(MouseInputInfo input)
        {
            return false;
        }

        private SKRectI CalculateRect(SKPointI p1, SKPointI p2)
        {
            var template = _instances.Template;
        
            var img = SelectedTileset.Get(null);
            var maxX = img?.Width ?? 0 / template.TileSize;
            var maxY = img?.Height ?? 0 / template.TileSize;
        
            var x1 = MathF.Min(maxX, MathF.Min(p1.X, p2.X));
            var x2 = MathF.Max(0, MathF.Max(p1.X, p2.X));
        
            var y1 = MathF.Min(maxY, MathF.Min(p1.Y, p2.Y));
            var y2 = MathF.Max(0, MathF.Max(p1.Y, p2.Y));

            return new SKRectI((int) x1, (int) y1, (int) x2 + 1, (int) y2 + 1);
        }
    }
}