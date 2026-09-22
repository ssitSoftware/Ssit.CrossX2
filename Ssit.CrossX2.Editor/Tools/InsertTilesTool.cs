using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia.Input;
using SkiaSharp;
using Ssit.CrossX2.Editor.Helpers;
using Ssit.CrossX2.Editor.Input;
using Ssit.CrossX2.Editor.Service;
using Ssit.CrossX2.Framework.Games.Map;

namespace Ssit.CrossX2.Editor.Tools
{
    public class InsertTilesTool : EditorTool
    {
        public new const string Name = "Insert Tiles";

        public HashSet<uint> UniqueTiles { get; } = new();

        public Tile[,] Tiles { get; private set; } = new Tile[0, 0];

        private bool _makeHalfOpacity;
        private SKRectI? _forbiddenArea;

        public InsertTilesTool(IEditorInstances instances) : base(Name, instances)
        {
        }
    
        public override void OnButtonDown(MouseInputInfo input)
        {
            base.OnButtonDown(input);

            if (input.ActionButton == MouseButton.Left)
            {
                _forbiddenArea = null;
                PlaceTiles(true);
                Editor.Redraw();
            }
        }

        public override void OnMouseMove(MouseInputInfo input)
        {
            base.OnMouseMove(input);

            if (input.MouseButtons == MouseButton.Left)
            {
                PlaceTiles(false);
            }

            _makeHalfOpacity = input.Modifiers.HasFlag(KeyModifiers.Shift);
            Editor.Redraw();
        }

        public void SetTiles(Tileset tileset, SKRectI selection)
        {
            var tilesContainer = Instances.TilesetsContainer;
            var tileIndex = Array.IndexOf(tilesContainer.TileSets, tileset);
            if (tileIndex < 0) throw new InvalidOperationException();

            UniqueTiles.Clear();

            Tiles = new Tile[selection.Width, selection.Height];

            for (var x = 0; x < selection.Width; ++x)
            {
                for (var y = 0; y < selection.Height; ++y)
                {
                    var materialName = tileset.Meta.GetMaterial(x + selection.Left, y + selection.Top);
                    int material = 0;
                    
                    for (var idx = 0; idx < Instances.Template.Materials.Length; ++idx)
                    {
                        if (Instances.Template.Materials[idx].Name == materialName)
                        {
                            material = idx;
                            break;
                        }
                    }
                    
                    Tiles[x, y] = new Tile(tileIndex, x + selection.Left, y + selection.Top, material);
                    UniqueTiles.Add(Tiles[x, y].Value);
                }
            }
        }

        private SKPointI? GetTargetPosition()
        {
            if (!MousePosition.HasValue) return null;

            var pos = Editor.ScreenToMap(MousePosition.Value);

            //pos.X -= (Tiles.GetLength(0)-1) / 2f;
            //pos.Y -= (Tiles.GetLength(1)-1) / 2f;

            return new SKPointI((int) pos.X, (int) pos.Y);
        }

        private void PlaceTiles(bool saveState)
        {
            var position = GetTargetPosition();
            if (!position.HasValue) return;

            var pos = position.Value;

            var layer = Editor.SelectedLayer;

            var rect = SKRectI.Create(pos.X, pos.Y, Tiles.GetLength(0), Tiles.GetLength(1));
            if (_forbiddenArea?.IntersectsWith(rect) ?? false) return;

            _forbiddenArea = rect;

            if (saveState)
            {
                Instances.UndoRedoServices.PushState();
            }
            
            for (var x = 0; x < Tiles.GetLength(0); ++x)
            {
                var xx = pos.X + x;

                if (xx < 0 || xx >= layer.Width) continue;

                for (var y = 0; y < Tiles.GetLength(1); ++y)
                {
                    var yy = pos.Y + y;
                    if (yy < 0 || yy >= layer.Height) continue;

                    var tile = Tiles[x, y];
                    layer.Tiles[xx, yy] = tile;
                }
            }

            Instances.Map.OnModified();
        }

        public override void Render(SKCanvas skCanvas, GRContext grContext, int width, int height)
        {
            base.Render(skCanvas, grContext, width, height);

            var template = Instances.Template;
        
            var ts = template.TileSize * Editor.Zoom.Value;
            var origTs = template.TileSize;

            var position = GetTargetPosition();

            if (!position.HasValue) return;
            var pos = new Vector2(position.Value.X, position.Value.Y);

            if (pos.X >= -Tiles.GetLength(0) && pos.Y >= -Tiles.GetLength(1) && pos.X < Editor.SelectedLayer.Width &&
                pos.Y < Editor.SelectedLayer.Height)
            {
                pos = Editor.MapToScreen(pos);

                var tilesets = Editor.GetTileSetImages(grContext);

                SkPaint.IsStroke = false;
                
                if (_makeHalfOpacity)
                {
                    SkPaint.Color = SKColors.White.WithAlpha(16);
                }
                else
                {
                    SkPaint.Color = SKColors.White.WithAlpha(32);
                }
                
                for (var x = 0; x < Tiles.GetLength(0); ++x)
                {
                    for (var y = 0; y < Tiles.GetLength(1); ++y)
                    {
                        var tile = Tiles[x, y];
                        if (tile.IsEmpty) continue;

                        skCanvas.DrawRect(SKRect.Create(pos.X + x * ts, pos.Y + y * ts, ts, ts), SkPaint);
                    }
                }
                
                if (_makeHalfOpacity)
                {
                    SkPaint.Color = SKColors.White.WithAlpha(128);
                }
                else
                {
                    SkPaint.Color = SKColors.White;
                }

                for (var x = 0; x < Tiles.GetLength(0); ++x)
                {
                    for (var y = 0; y < Tiles.GetLength(1); ++y)
                    {
                        var tile = Tiles[x, y];
                        if (tile.IsEmpty) continue;

                        var img = tilesets[tile.TileSet];
                        if (img is null) continue;

                        skCanvas.DrawImage(img,
                            SKRect.Create(tile.X * origTs, tile.Y * origTs, origTs, origTs),
                            SKRect.Create(pos.X + x * ts, pos.Y + y * ts, ts, ts),
                            SkPaint);
                    }
                }
            }
        }
    }
}