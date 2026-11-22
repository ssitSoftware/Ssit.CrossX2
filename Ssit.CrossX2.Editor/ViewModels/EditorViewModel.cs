using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using SkiaSharp;
using Ssit.CrossX2.Editor.Helpers;
using Ssit.CrossX2.Editor.Input;
using Ssit.CrossX2.Editor.Models;
using Ssit.CrossX2.Editor.Service;
using Ssit.CrossX2.Editor.Tools;
using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.Games.Editor;
using Ssit.CrossX2.Framework.Games.Map;
using Ssit.CrossX2.Framework.Games.Utils;

namespace Ssit.CrossX2.Editor.ViewModels
{
    public class EditorViewModel: ViewModelBase, ISkRenderer, IEditor
    {
        private readonly IEditorInstances _instances;
        private readonly IEditorBitmapsProvider _bitmapsProvider;
        private readonly EditorData _editorData;
        
        public event Action RedrawNeeded;
    
        private readonly List<SKImage> _imagesCache = new();

        public Vector2 Size { get; } = Vector2.Zero;

        private readonly Stopwatch _stopwatch = new();

        private readonly SKPoint[] _arrowPoints = new SKPoint[3];
        
        private static readonly SKColor[] LinkColors =
        [
            SKColors.DodgerBlue,
            SKColors.LightSeaGreen,
            SKColors.LightBlue,
            SKColors.DarkSeaGreen,
            SKColors.MediumSeaGreen
        ];
        
        public int SelectedMaterial
        {
            get => Tools.GetTool<SetMaterialTool>().MaterialIndex;
            set => Tools.GetTool<SetMaterialTool>().MaterialIndex = value;
        }

        // Replaced as a whole on every change (never mutated), so the render thread can read it safely.
        private HashSet<int> _selectedObjects = new();

        public int SelectedObject
        {
            get
            {
                var selection = _selectedObjects;
                return selection.Count == 1 ? selection.First() : 0;
            }
            set => SetSelectedObjects(value == 0 ? Array.Empty<int>() : new[] { value });
        }

        public IReadOnlyCollection<int> SelectedObjects => _selectedObjects;

        public bool IsObjectSelected(int id) => id != 0 && _selectedObjects.Contains(id);

        public void SetSelectedObjects(IEnumerable<int> ids)
        {
            var selection = new HashSet<int>(ids);
            selection.Remove(0);

            if (selection.SetEquals(_selectedObjects))
                return;

            _selectedObjects = selection;

            OnPropertyChanged(nameof(SelectedObject));
            OnPropertyChanged(nameof(SelectedObjects));

            // With several objects selected SelectedObject is 0, so the properties show the map and layer only.
            if (PropertiesView is not null)
            {
                PropertiesView.MapFile = _instances.Map;
            }

            Redraw();
        }

        public void AddToSelection(int id) => SetSelectedObjects(_selectedObjects.Append(id));

        public void RemoveFromSelection(int id) => SetSelectedObjects(_selectedObjects.Where(o => o != id));

        public MapLayer SelectedLayer
        {
            get;
            set
            {
                var oldLayer = field;

                if (value is null)
                    return;

                if (SetField(ref field, value))
                {
                    if (oldLayer != null && field != null)
                    {
                        var offset =
                            LayerOffsetCalculator.CalculateGlobalOffset(oldLayer, _instances.Map.MainLayer, Offset);
                        Offset = LayerOffsetCalculator.CalculateLayerOffset(SelectedLayer, _instances.Map.MainLayer,
                            offset);
                    }

                    PropertiesView.MapFile = _instances.Map;

                    if (field is not null)
                    {
                        _editorData.SelectedLayer = field.Id;
                        _editorData.RequestSave();
                    }

                    UpdateMousePosition(_mouseScreenPosition);
                    Tools?.Current?.OnLayerChanged();
                    Redraw();
                }
            }
        }

        public PropertiesViewModel PropertiesView { get; }

        private const string NoMousePositionInfo = "-";

        private Vector2? _mouseScreenPosition;

        /// <summary>Tile under the mouse cursor in the selected layer (status bar).</summary>
        public string MouseTileInfo
        {
            get;
            private set => SetField(ref field, value);
        } = NoMousePositionInfo;

        /// <summary>Exact position of the mouse cursor in the selected layer, in tiles (status bar).</summary>
        public string MousePositionInfo
        {
            get;
            private set => SetField(ref field, value);
        } = NoMousePositionInfo;

        public IEditorTools Tools { get; }

        // Objects drawn on the selected layer with their screen bounds. A frame fills _currentObjects (render
        // thread) and publishes it as _lastFrameObjects once all layers are drawn; hit tests (clicks on the UI
        // thread) read only the published list, so they never see a frame that is half drawn.
        private List<MapObjectInfo> _currentObjects = new();
        private volatile List<MapObjectInfo> _lastFrameObjects = new();

        public bool IsFullscreen
        {
            get;
            set
            {
                if (SetField(ref field, value))
                {
                    SelectedTool = value ? EmptyTool.Name : SelectionTool.Name;
                }
            }
        }

        public string SelectedTool
        {
            get => Tools.Current?.Name;

            set
            {
                var tool = Tools.GetTool(value);

                // Switching to the selection tool keeps the selected objects (from the region selection, or
                // placed from a prefab) so they can be moved; choosing it while it's active (Escape) clears them.
                var keepSelection = tool is SelectionTool && Tools.Current is not SelectionTool;

                tool.Reset();
                if (!keepSelection)
                {
                    SelectedObject = 0;
                }
                Tools.Current = tool;
                OnPropertyChanged();
            }
        }

        public ICommand SetToolCommand { get; }

        /// <summary>A region selection also selects objects on the other layers with the same speed.</summary>
        public bool SelectObjectsOnAllLayers
        {
            get => _editorData.SelectObjectsOnAllLayers;
            set
            {
                if (_editorData.SelectObjectsOnAllLayers == value) return;

                _editorData.SelectObjectsOnAllLayers = value;
                _editorData.RequestSave();
                OnPropertyChanged();

                Tools.GetTool<SelectionTool>().RefreshRegionSelection();
            }
        }

        public bool ShowLinks
        {
            get => _editorData.ShowLinks;
            set
            {
                if (value != _editorData.ShowLinks)
                {
                    _editorData.ShowLinks = value;
                    _editorData.RequestSave();
                    
                    OnPropertyChanged();
                    Redraw();
                }
            }
        }
        
        public bool ShowMaterials
        {
            get => _editorData.ShowMaterials;
            set
            {
                if (value != _editorData.ShowMaterials)
                {
                    _editorData.ShowMaterials = value;
                    _editorData.RequestSave();
                    
                    OnPropertyChanged();
                    Redraw();
                }
            }
        }
        
        public bool ShowCollisions
        {
            get => _editorData.ShowCollisions;
            set
            {
                if (value != _editorData.ShowCollisions)
                {
                    _editorData.ShowCollisions = value;
                    _editorData.RequestSave();
                    
                    OnPropertyChanged();
                    Redraw();
                }
            }
        }
        

        public bool ShowAllLayers
        {
            get => _editorData.ShowAllLayers;
            set
            {
                if (value != _editorData.ShowAllLayers)
                {
                    _editorData.ShowAllLayers = value;
                    _editorData.RequestSave();
                    
                    OnPropertyChanged();
                    Redraw();
                }
            }
        }

        public bool ShowGrid
        {
            get => _editorData.ShowGrid;
            set
            {
                if (value != _editorData.ShowGrid)
                {
                    _editorData.ShowGrid = value;
                    _editorData.RequestSave();
                    
                    OnPropertyChanged();
                    Redraw();
                }
            }
        }

        public ZoomViewModel Zoom { get; }
    
        private SKPaint _skPaint = new();
    
        private Vector2 _currentSize;

        private Dictionary<int, SKColorFilter> _colorFilters = new();

        public Vector2 Offset
        {
            get => new(_editorData.CameraX, _editorData.CameraY);
            set
            {
                _editorData.CameraX = value.X;
                _editorData.CameraY = value.Y;
                _editorData.RequestSave();
                
                OnPropertyChanged();
                Redraw();
            }
        }

        public bool OnionMode
        {
            get => _editorData.OnionMode;
            set
            {
                if (_editorData.OnionMode != value)
                {
                    _editorData.OnionMode = value;
                    _editorData.RequestSave();
                    
                    OnPropertyChanged();
                    Redraw();
                }
            }
        }

        public bool Animate
        {
            get => _editorData.Animate;
            set
            {
                if (_editorData.Animate != value)
                {
                    _editorData.Animate = value;
                    _editorData.RequestSave();
                    
                    OnPropertyChanged();
                    Redraw();

                    EnableAnimation(value);
                }
            }
        }

        public bool ShowObjects
        {
            get => _editorData.ShowObjects;
            set
            {
                if(value != _editorData.ShowObjects)
                {
                    _editorData.ShowObjects = value;
                    _editorData.RequestSave();
                    
                    OnPropertyChanged();
                    Redraw();
                }
            }
        }

        public EditorViewModel(IEditorInstances instances, IServices services, IEditorBitmapsProvider bitmapsProvider, EditorData editorData)
        {
            _instances = instances;
            _bitmapsProvider = bitmapsProvider;
            _editorData = editorData;
            Tools = _instances.Tools;

            _instances.SetEditor(this);

            SelectedTool = SelectionTool.Name;
            SetToolCommand = new RelayCommand<string>(s => SelectedTool = s);
            Zoom = new ZoomViewModel(() => RedrawNeeded?.Invoke(), editorData);
            
            Tools.PropertyChanged += (_,_) =>
            {
                if (Tools.Current is not SelectionTool)
                {
                    SelectedObject = 0;
                }
                OnPropertyChanged(nameof(SelectedTool));
            };
            
            EnableAnimation(_editorData.Animate);
            PropertiesView = services.Create<PropertiesViewModel>(this);
        }

        private void EnableAnimation(bool value)
        {
            if (value)
            {
                _stopwatch.Restart();
            }
            else
            {
                _stopwatch.Stop();
                _stopwatch.Reset();
            }
        }
        
        private SKColorFilter GetFilter(RgbaColor color)
        {
            if (!_colorFilters.TryGetValue(color.ToInt32(), out var filter))
            {
                filter = SKColorFilter.CreateColorMatrix(
                [
                    color.Rf, 0, 0, 0, 0,
                    0, color.Gf, 0, 0, 0,
                    0, 0, color.Bf, 0, 0,
                    0, 0, 0, color.Af, 0
                ]);
                _colorFilters.Add(color.ToInt32(), filter);
            }

            return filter;
        }

        public void Render(SKCanvas skCanvas, GRContext grContext, int width, int height)
        {
            EnsurePanInBounds();
        
            // A new list: the published one may still be read by a hit test.
            _currentObjects = new List<MapObjectInfo>(_lastFrameObjects.Count);
            
            _currentSize = new Vector2(width, height);

            var start = MapToScreen(Vector2.Zero);
            var end = MapToScreen(new Vector2(SelectedLayer.Width, SelectedLayer.Height));
        
            var ts = _instances.Template.TileSize * Zoom.Value;
        
            var w = _instances.Template.TargetSize.Width * Zoom.Value;
            var h = _instances.Template.TargetSize.Height * Zoom.Value;
        
            var x = (width-w) / 2;
            var y = (height-h) / 2;

            if (ShowAllLayers)
            {
                skCanvas.Save();
                skCanvas.ClipRect(SKRect.Create(x, y, w, h), SKClipOperation.Difference);
                skCanvas.Clear(IsFullscreen ? SKColors.Black : new SKColor(20,20,20));
            
                skCanvas.Restore();
                skCanvas.Save();
                skCanvas.ClipRect(SKRect.Create(x, y, w, h));
            }
        
            _skPaint.IsStroke = false;
            _skPaint.StrokeWidth = 1;
            _skPaint.Color = ShowAllLayers
                ? _instances.Template.GameBackground.ToSkia()
                : _instances.Template.EditorLayerBackground.ToSkia();
        
            skCanvas.DrawRect(new SKRect(start.X, start.Y, end.X, end.Y), _skPaint);

            _skPaint.Color = SKColors.White;
        
            if (ShowAllLayers || OnionMode || IsFullscreen)
            {
                var mapFile = _instances.Map;
            
                for (var idx = 0; idx < mapFile.Layers.Count; ++idx)
                {
                    var layer = mapFile.Layers[idx];

                    byte alpha = 255;
                    if (!ShowAllLayers && !IsFullscreen)
                    {
                        if (layer.HorizontalSpeed == 0 ||
                            layer.VerticalSpeed == 0)
                        {
                            continue;
                        }

                        if (OnionMode)
                        {
                            if ( Math.Abs(layer.HorizontalSpeed - SelectedLayer.HorizontalSpeed) > float.Epsilon ||
                                Math.Abs(layer.VerticalSpeed - SelectedLayer.VerticalSpeed) > float.Epsilon)
                            {
                                continue;
                            }
                        }

                        if (layer != SelectedLayer)
                        {
                            alpha = 128;
                        }
                    }
                    
                    var color = new RgbaColor(layer.TintColor.R, layer.TintColor.G, layer.TintColor.B, (byte)(alpha * layer.TintColor.A / 255));
                    
                    _skPaint.Color = SKColors.White;
                    _skPaint.ColorFilter = GetFilter(color);
                    
                    RenderLayer(layer, skCanvas, grContext, width, height);
                    _skPaint.ColorFilter = null;

                    if (layer.FogColor.A > 0 && (ShowAllLayers || IsFullscreen ))
                    {
                        _skPaint.Color = layer.FogColor.ToSkia();
                        _skPaint.IsStroke = false;
                        skCanvas.DrawRect(x, y, w, h, _skPaint);
                        _skPaint.Color = SKColors.White;
                    }
                }
            }
            else
            {
                _skPaint.Color = SKColors.White;
                if (SelectedLayer.TintColor != RgbaColor.White)
                {
                    _skPaint.ColorFilter = GetFilter(SelectedLayer.TintColor);
                }
                RenderLayer(SelectedLayer, skCanvas, grContext, width, height);
                _skPaint.ColorFilter = null;
            }

            _skPaint.Color = SKColors.White;

            // All objects of the frame are known now (the tool's own rendering hit-tests them, too).
            _lastFrameObjects = _currentObjects;

            Tools.Current.Render(skCanvas, grContext, width, height);

            if (ShowGrid && !IsFullscreen)
            {
                _skPaint.IsStroke = true;
                _skPaint.StrokeWidth = 1;
                _skPaint.Color = SKColors.White.WithAlpha(32);
            
                var xl = (int) ((end.X - start.X) / ts + 1);
                var yl = (int) ((end.Y - start.Y) / ts + 1);

                for (var idx = 0; idx < xl; ++idx)
                {
                    skCanvas.DrawLine(idx * ts + start.X, start.Y, idx* ts + start.X, end.Y, _skPaint);
                }
            
                for (var idx = 0; idx < yl; ++idx)
                {
                    skCanvas.DrawLine(start.X, idx * ts + start.Y, end.X, idx* ts + start.Y, _skPaint);
                }
            }
            
            if (_instances.Template.DisplayScreenGridVertical)
            {
                var st = MapToScreen(Vector2.Zero);
                var en = MapToScreen(new Vector2(SelectedLayer.Width, SelectedLayer.Height));
             
                _skPaint.IsStroke = true;
                _skPaint.StrokeWidth = 1;
                _skPaint.Color = SKColors.White.WithAlpha(96);
                
                var xl = (int)MathF.Ceiling(SelectedLayer.Width * ts / w);
                for (var idx = 0; idx <= xl; ++idx)
                {
                    skCanvas.DrawLine(st.X + idx * w, st.Y, st.X + idx * w, en.Y, _skPaint);
                }
            }
        
            if (!ShowAllLayers && !IsFullscreen)
            {
                _skPaint.IsStroke = true;
                _skPaint.StrokeWidth = 1;
                _skPaint.Color = SKColors.Yellow.WithAlpha(128);

                skCanvas.DrawRect(x, y, w, h, _skPaint);
            }
            else
            {
                skCanvas.Restore();
            }

            if (Animate)
            {
                Redraw();
            }
        }
    
        private void RenderLayer(MapLayer layer, SKCanvas skCanvas, GRContext grContext, int width, int height)
        {
            if (layer == null) return;

            bool current = SelectedLayer == layer;

            var template = _instances.Template;
        
            var origTs = template.TileSize;
            var ts = template.TileSize * Zoom.Value;

            bool showObjects = ShowObjects;
            
            var offset = Offset;
            if (layer != SelectedLayer)
            {
                offset = LayerOffsetCalculator.CalculateGlobalOffset(SelectedLayer, _instances.Map.MainLayer, Offset);
                offset = LayerOffsetCalculator.CalculateLayerOffset(layer, _instances.Map.MainLayer, offset);
            }
            
            var start = MapToScreen(Vector2.Zero, offset);
            
            RenderObjects(layer, skCanvas, grContext, showObjects, start, true);
            
            var tileSetImages = GetTileSetImages(grContext);
        
            for (var x = 0; x < layer.Width; ++x)
            {
                var posX = start.X + x * ts;
            
                if (posX + ts < 0 || posX > width) continue;
            
                for (var y = 0; y < layer.Height; ++y)
                {
                    var posY = start.Y + y * ts;

                    if (posY + ts < 0 || posY > height) continue;
                
                    var tile = layer.Tiles[x, y];
                    if (tile.IsEmpty) continue;

                    var img = tileSetImages[tile.TileSet];
                    if (img is null) continue;

                    const float epsilon = 0.0001f;
                    
                    var src = SKRect.Create(tile.X * origTs + epsilon, tile.Y * origTs + epsilon, origTs-epsilon * 2, origTs - epsilon * 2); 
                    var dest = SKRect.Create(posX, posY, ts, ts);
                
                    skCanvas.DrawImage(img, src, dest, _skPaint);
                }
            }

            if (!IsFullscreen && ShowCollisions && layer == _instances.Map.MainLayer)
            {
                _skPaint.IsStroke = false;
                _skPaint.Color = SKColors.Red.WithAlpha(128);
                
                for (var x = 0; x < layer.Width; ++x)
                {
                    var posX = start.X + x * ts;

                    if (posX + ts < 0 || posX > width) continue;

                    for (var y = 0; y < layer.Height; ++y)
                    {
                        var posY = start.Y + y * ts;
                        
                        if (posY + ts < 0 || posY > height) continue;

                        var tile = layer.Tiles[x, y];
                        if (tile.IsEmpty) continue;

                        var tileset = _instances.TilesetsContainer.TileSets[tile.TileSet];

                        if (tileset.HasMeta)
                        {
                            if (tileset.GetCollissionPolygon(tile.X, tile.Y, ts, posX, posY, out var polygon))
                            {
                                skCanvas.DrawPoints(SKPointMode.Polygon, polygon, _skPaint);
                            }
                        }
                        else if (tileset.HasMask)
                        {
                            var materialIndex = Math.Clamp(tile.Material, 0, template.Materials.Length - 1);
                            var tint = template.Materials[materialIndex].PreviewColor.WithOpacity(0.5f).ToSkia();

                            tileset.DrawMask(skCanvas, grContext, tile.X, tile.Y, origTs,
                                SKRect.Create(posX, posY, ts, ts), tint);
                        }
                    }
                }

                _skPaint.Color = SKColors.White;
            }
            
            if (current && ShowMaterials && !IsFullscreen && layer == _instances.Map.MainLayer)
            {
                for (var x = 0; x < layer.Width; ++x)
                {
                    var posX = start.X + x * ts;

                    if (posX + ts < 0 || posX > width) continue;

                    for (var y = 0; y < layer.Height; ++y)
                    {
                        var posY = start.Y + y * ts;

                        if (posY + ts < 0 || posY > height) continue;

                        var tile = layer.Tiles[x, y];
                        if (tile.IsEmpty) continue;

                        var material = tile.Material;

                        if (material > 0 && material < _bitmapsProvider.MaterialsPreview.Length)
                        {
                            var img = _bitmapsProvider.MaterialsPreview[material];
                            if (img is null) continue;

                            var src = SKRect.Create(0, 0, img.Width, img.Height);
                            var dest = SKRect.Create(posX, posY, ts, ts);

                            skCanvas.DrawImage(img, src, dest, _skPaint);
                        }
                    }
                }
            }

            RenderObjects(layer, skCanvas, grContext, showObjects, start, false);
            
            if (!IsFullscreen && current && layer == _instances.Map.MainLayer && ShowLinks && showObjects)
            {
                _skPaint.IsStroke = true;

                var transfomArrow1 = Matrix3x2.CreateRotation(MathF.PI / 6f);
                var transfomArrow2 = Matrix3x2.CreateRotation(-MathF.PI / 6f);

                float zoom = Zoom.Value;
                float arrowSize = MathF.Max(16, MathF.Min(64, 8 * zoom));
                
                for (var idx = 0; idx < layer.Objects.Count; ++idx)
                {
                    var obj = layer.Objects[idx];

                    if (obj.Links is null) continue;
                    
                    for (var li = 0; li < obj.Links.Count; ++li)
                    {
                        var otherObj = layer.FindObject(obj.Links[li]);

                        if (otherObj != null)
                        {
                            var p1 = MapToScreen(obj.Position);
                            var p2 = MapToScreen(otherObj.Position);

                            _skPaint.Color = LinkColors[(li+obj.Id) % LinkColors.Length];

                            var linkType = GetLinkType(obj.ParametersObject?.GetType(), li);

                            if (linkType == EditorLinkType.Circular)
                            {
                                _skPaint.IsStroke = true;
                                _skPaint.StrokeWidth = 1;

                                skCanvas.DrawCircle(p1.ToSkia(), (p2 - p1).Length(), _skPaint);
                                continue;
                            }

                            if (linkType == EditorLinkType.Area)
                            {
                                _skPaint.IsStroke = true;
                                _skPaint.StrokeWidth = 1;

                                // Top left corner at the object, bottom right at the linked object (normalized,
                                // so the area is drawn even if the linked object is placed above or left of it).
                                var topLeft = Vector2.Min(p1, p2);
                                var bottomRight = Vector2.Max(p1, p2);

                                skCanvas.DrawRect(SKRect.Create(topLeft.X, topLeft.Y,
                                    bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y), _skPaint);
                                continue;
                            }

                            var arrow1 = Vector2.TransformNormal(Vector2.Normalize(p1 - p2), transfomArrow1) * arrowSize;
                            var arrow2 = Vector2.TransformNormal(Vector2.Normalize(p1 - p2), transfomArrow2) * arrowSize;

                            _arrowPoints[0] = p2.ToSkia();
                            _arrowPoints[1] = (p2 + arrow1).ToSkia();
                            _arrowPoints[2] = (p2 + arrow2).ToSkia();

                            _skPaint.IsStroke = true;
                            _skPaint.StrokeWidth = 1;

                            skCanvas.DrawLine(p1.ToSkia(), p2.ToSkia(), _skPaint);

                            _skPaint.IsStroke = false;
                            skCanvas.DrawVertices(SKVertexMode.Triangles, _arrowPoints, null, _skPaint);
                        }
                    }
                }
                
                _skPaint.StrokeWidth = 1;
            }
        }

        private static readonly Dictionary<Type, EditorLinkType[]> LinkTypesCache = new();

        /// <summary>
        /// How the link with the <paramref name="index"/> of <see cref="MapObject.Links"/> is displayed: taken from
        /// the <see cref="EditorLinkAttribute"/> of the matching property of the parameters type. The links are
        /// the int properties with the attribute, in the same order as <see cref="MapObject.UpdateLinks"/> collects them.
        /// </summary>
        private static EditorLinkType GetLinkType(Type parametersType, int index)
        {
            if (parametersType is null)
                return EditorLinkType.Point;

            if (!LinkTypesCache.TryGetValue(parametersType, out var linkTypes))
            {
                linkTypes = parametersType
                    .GetProperties(BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public)
                    .Where(p => p.PropertyType == typeof(int))
                    .Select(p => p.GetCustomAttribute<EditorLinkAttribute>())
                    .Where(a => a is not null)
                    .Select(a => a.LinkType)
                    .ToArray();

                LinkTypesCache[parametersType] = linkTypes;
            }

            return index < linkTypes.Length ? linkTypes[index] : EditorLinkType.Point;
        }

        private void RenderObjects(MapLayer layer, SKCanvas skCanvas, GRContext grContext, bool showObjects, Vector2 start, bool behind)
        {
            bool current = SelectedLayer == layer;
            
            for (var idx = 0; idx < layer.Objects.Count; )
            {
                var obj = layer.Objects[idx];

                if (behind && obj.ZOrder >= 0)
                {
                    ++idx;
                    continue;
                }

                if (!behind && obj.ZOrder < 0)
                {
                    ++idx;
                    continue;
                }

                if (obj.HasLogic && !showObjects)
                {
                    ++idx;
                    continue;
                }

                try
                {
                    var editorImg = obj.HasLogic
                        ? _instances.ObjectsContainer.Get(obj.TypeId)
                        : _instances.ImagesContainer.Get(obj.TypeId);

                    var rect = DrawObject(obj, editorImg, obj.Flipped, obj.Position, start, skCanvas, grContext,
                        _skPaint, layer);

                    if (current)
                    {
                        _currentObjects.Add(new MapObjectInfo
                        {
                            Object = obj,
                            ScreenBounds = rect
                        });
                    }
                }
                catch (KeyNotFoundException)
                {
                    layer.Objects.RemoveAt(idx);
                    continue;
                }

                ++idx;
            }
        }

        private RectangleF DrawObject(MapObject mapObject, EditorImage editorImg, bool flipped, Vector2 position, Vector2 start, SKCanvas skCanvas,
            GRContext grContext, SKPaint skPaint, MapLayer layer)
        {
            var ts = _instances.Template.TileSize * Zoom.Value;
            var scale = Zoom.Value;
            
            var oldFilter = skPaint.ColorFilter;

            var timeOffset = position.X + position.Y;
            if (mapObject?.ParametersObject is StaticObjectParameters sop)
            {
                timeOffset = sop.AnimationTimeOffsetInMs / 1000f;
            }
            
            var time = Animate ? (float) (_stopwatch.Elapsed.TotalSeconds + timeOffset) : 0;
            var frame = editorImg.Sprite.GetFrameForTime(editorImg.Sequence, time);

            var img = editorImg.Sprite.Get(grContext);

            var origin = frame.Offset + editorImg.Sprite.Origin;
            var pos = position * ts + start;

            var rect = new RectangleF(pos.X - origin.X * scale, pos.Y - origin.Y * scale, frame.Source.Width * scale, frame.Source.Height * scale);

            var destRect = rect.ToSkia();
            var srcRect = SKRect.Create(frame.Source.X, frame.Source.Y, frame.Source.Width, frame.Source.Height);
            
            if (flipped)
            {
                skCanvas.Save();
                skCanvas.Scale(-1, 1, pos.X, 0);
                skCanvas.DrawImage(img, srcRect, destRect, skPaint);
                skCanvas.Restore();
                
                rect = new RectangleF(pos.X - (frame.Source.Width - origin.X) * scale, pos.Y - origin.Y * scale, frame.Source.Width * scale, frame.Source.Height * scale);
            }
            else
            {
                skCanvas.DrawImage(img, srcRect, destRect, skPaint);
            }
            
            if (mapObject is not null && IsObjectSelected(mapObject.Id))
            {
                skPaint.ColorFilter = null;
                skPaint.Color = SKColors.Orange.WithAlpha(192);
                skPaint.IsStroke = true;

                skCanvas.DrawRect(rect.ToSkia(), skPaint);

                skPaint.Color = SKColors.GreenYellow.WithAlpha(192);
                skCanvas.DrawLine(pos.X + 10, pos.Y + 10, pos.X - 10, pos.Y - 10, skPaint);
                skCanvas.DrawLine(pos.X + 10, pos.Y - 10, pos.X - 10, pos.Y + 10, skPaint);
            }
            else if (layer == SelectedLayer && !IsFullscreen && (!ShowAllLayers || ShowGrid))
            {
                skPaint.ColorFilter = null;
                skPaint.Color = SKColors.Orange.WithAlpha(64);
                skPaint.IsStroke = true;

                skCanvas.DrawRect(rect.ToSkia(), skPaint);
            }

            skPaint.Color = SKColors.White;
            skPaint.ColorFilter = oldFilter;
            
            return rect;
        }

        public RectangleF DrawEditorImage(EditorImage image, bool flipped, Vector2 position, SKCanvas skCanvas, GRContext grContext, SKPaint skPaint)
        {
            var start = MapToScreen(Vector2.Zero);
            return DrawObject(null, image, flipped, position, start, skCanvas, grContext, skPaint, SelectedLayer);
        }

        public Vector2 ScreenToMap(Vector2 position) => ScreenToMap(position, Offset);
        
        private Vector2 ScreenToMap(Vector2 position, Vector2 offset)
        {
            var template = _instances.Template;
            var ts = template.TileSize * Zoom.Value;
            
            position += new Vector2(template.TargetSize.Width / 2f, -template.TargetSize.Height / 2f) * Zoom.Value;
            return offset + (position - _currentSize / 2) / ts;
        }

        public Vector2 MapToScreen(Vector2 position) => MapToScreen(position, Offset);
        
        private Vector2 MapToScreen(Vector2 position, Vector2 offset)
        {
            var template = _instances.Template;
            var ts = template.TileSize * Zoom.Value;
            return (position - offset) * ts + _currentSize / 2 + new Vector2(-template.TargetSize.Width / 2f, template.TargetSize.Height / 2f) * Zoom.Value;
        }

        public void UpdateMousePosition(Vector2? screenPosition)
        {
            _mouseScreenPosition = screenPosition;

            if (screenPosition is null || SelectedLayer is null || _currentSize == Vector2.Zero)
            {
                MouseTileInfo = NoMousePositionInfo;
                MousePositionInfo = NoMousePositionInfo;
                return;
            }

            var mapPosition = ScreenToMap(screenPosition.Value);

            var tileX = (int)MathF.Floor(mapPosition.X);
            var tileY = (int)MathF.Floor(mapPosition.Y);

            var insideLayer = tileX >= 0 && tileY >= 0 && tileX < SelectedLayer.Width && tileY < SelectedLayer.Height;

            MouseTileInfo = insideLayer
                ? string.Create(CultureInfo.InvariantCulture, $"{tileX}, {tileY}")
                : NoMousePositionInfo;

            MousePositionInfo = string.Create(CultureInfo.InvariantCulture, $"{mapPosition.X:0.00}, {mapPosition.Y:0.00}");
        }

        public void EnsurePanInBounds()
        {
            if (SelectedLayer == null)
                return;
            
            var template = _instances.Template;

            var maxX = SelectedLayer.Width - (float)template.TargetSize.Width / template.TileSize;
            var minX = 0;

            var maxY = SelectedLayer.Height;
            var minY = (float)template.TargetSize.Height / template.TileSize;

            var ox = Math.Max(minX, Math.Min(maxX, Offset.X));
            var oy = Math.Max(minY, Math.Min(maxY, Offset.Y));

            if (float.IsNaN(ox))
            {
                ox = (minX + maxX) / 2;
            }
            
            if (float.IsNaN(oy))
            {
                oy = (minY + maxY) / 2;
            }
            
            Offset = new Vector2(ox, oy);
        }
    
        public void Redraw() => RedrawNeeded?.Invoke();

        public IList<SKImage> GetTileSetImages(GRContext context)
        {
            _imagesCache.Clear();
            var tilesets = _instances.TilesetsContainer.TileSets;

            for (var idx = 0; idx < tilesets.Length; ++idx)
            {
                _imagesCache.Add(tilesets[idx].Get(context));
            }

            return _imagesCache;
        }

        public void GetMapObjects(IList<MapObjectInfo> buffer, Vector2 screenPosition)
        {
            var objects = _lastFrameObjects;

            buffer.Clear();
            for (var idx = 0; idx < objects.Count; ++idx)
            {
                if (objects[idx].ScreenBounds.Contains(screenPosition))
                {
                    buffer.Add(objects[idx]);
                }
            }
        }

        public void GetMapObjects(IList<MapObjectInfo> buffer, RectangleF screenRect)
        {
            var objects = _lastFrameObjects;

            buffer.Clear();
            for (var idx = 0; idx < objects.Count; ++idx)
            {
                if (objects[idx].ScreenBounds.IsIntersecting(screenRect))
                {
                    buffer.Add(objects[idx]);
                }
            }
        }

        public void UnloadResources()
        {
        }
    }
}