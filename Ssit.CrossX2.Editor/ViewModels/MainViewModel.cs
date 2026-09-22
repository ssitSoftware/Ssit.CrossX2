using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Ssit.CrossX2.Editor.Helpers;
using Ssit.CrossX2.Editor.Models;
using Ssit.CrossX2.Editor.Service;
using Ssit.CrossX2.Editor.Tools;
using Ssit.CrossX2.Framework.Commands;
using Ssit.CrossX2.Framework.Games.Map;
using Ssit.CrossX2.Framework.Games.Template;

namespace Ssit.CrossX2.Editor.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IEditorInstances _instances;
        private readonly IFileService _fileService;
        private readonly IWindowService _windowService;

        public EditorViewModel EditorViewModel { get; }
        public TilesetSelectorViewModel TilesetSelectorViewModel { get; }

        public ImageSelectorViewModel ObjectsSelector { get; }
        public ImageSelectorViewModel ImagesSelector { get; }
        
        public ITilesetsContainer TilesetsContainer { get; }
    
        public MenuItemModel[] Menu { get; }
        public IEditorInstances Instances => _instances;

        public bool IsModified
        {
            get;
            set
            {
                if (SetField(ref field, value))
                {
                    SaveCommand.NotifyCanExecuteChanged();
                    UpdateTitle();
                }
            }
        }

        public ICommand NewCommand { get; }
        public ICommand OpenCommand { get; }

        public ICommand EscapeCommand { get; }
        public ICommand HotkeyCommand { get; }
        
        public ICommand EnterFullscreenCommand { get; }
        public IAsyncRelayCommand SaveCommand { get; }
        public IAsyncRelayCommand SaveAsCommand { get; }
        public ICommand HorizontalFlipCommand { get; }
        
        public MapFile MapFile
        {
            get => _mapFile;
            private set
            {
                var oldMap = _mapFile;
                
                if (SetField(ref _mapFile, value))
                {
                    _instances.SetMap(_mapFile);
                    
                    EditorViewModel.SelectedLayer =
                        MapFile.Layers.FirstOrDefault( o=> o.Id.ToLowerInvariant() == _editorData.SelectedLayer) ??
                        MapFile.Layers.FirstOrDefault( o=> o.Id.ToLowerInvariant() == "main") ??
                        MapFile.Layers.First();
                    
                    EditorViewModel.Redraw();
                    IsModified = _mapFile.IsModified;

                    if (oldMap is not null)
                    {
                        oldMap.PropertyChanged -= MapOnPropertyChanged;
                    }

                    _mapFile.PropertyChanged += MapOnPropertyChanged;
                    _instances.UndoRedoServices.Clear();
                    UpdateTitle();
                }
            }
        }

        public string Title
        {
            get;
            set => SetField(ref field, value);
        }

        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        public AsyncCommand RunCommand { get; }

        private string _filePath;
        private MapFile _mapFile;

        private readonly EditorData _editorData;
        
        public MainViewModel(IServices services, 
            IEditorInstances instances, 
            IFileService fileService,
            IObjectsContainer objectsContainer,
            IImagesContainer imagesContainer,
            IWindowService windowService,
            EditorData editorData)
        {
            _instances = instances;
            _fileService = fileService;
            _windowService = windowService;
            _editorData = editorData;
            
            ObjectsSelector = services.Create<ImageSelectorViewModel>(new ImageSelectorViewModel.Parameters
            {
                Categories = objectsContainer.Categories,
                Images = objectsContainer.Objects,
                SelectionChanged = s =>
                {
                    var tool = _instances.Tools.GetTool<InsertObjectTool>();
                    tool.Object = s as EditorObject;
                    _instances.Tools.Current = tool;
                    _instances.Editor.Redraw();
                }
            });
            
            ImagesSelector = services.Create<ImageSelectorViewModel>(new ImageSelectorViewModel.Parameters
            {
                Categories = imagesContainer.Categories,
                Images = imagesContainer.Images,
                SelectionChanged = s =>
                {
                    var tool = _instances.Tools.GetTool<InsertImageTool>();
                    tool.Image = s;
                    _instances.Tools.Current = tool;
                    _instances.Editor.Redraw();
                }
            });

            NewCommand = new AsyncRelayCommand(New);
            OpenCommand = new AsyncRelayCommand(Open);
            SaveCommand = new AsyncRelayCommand(Save, () => IsModified);
            SaveAsCommand = new AsyncRelayCommand(SaveAs);

            UndoCommand = new RelayCommand(() => _instances.UndoRedoServices.Undo());
            RedoCommand = new RelayCommand(() => _instances.UndoRedoServices.Redo());

            EscapeCommand = new RelayCommand(() =>
            {
                EditorViewModel.IsFullscreen = false;
                EditorViewModel.SelectedTool = SelectionTool.Name;
            });

            HotkeyCommand = new RelayCommand<HotkeyActions>(a => _instances.Tools.Current.OnHotkeyAction(a));
            
            EnterFullscreenCommand = new RelayCommand(() => EditorViewModel.IsFullscreen = true);

            RunCommand = new AsyncCommand( () => Run(), CanRun);
            
            HorizontalFlipCommand = new RelayCommand(() =>
            {
                if (_instances.Tools.Current is InsertImageTool tool)
                {
                    tool.Flipped = !tool.Flipped;
                }
                else if (_instances.Editor.SelectedObject != 0)
                {
                    var obj = _mapFile.FindObject(instances.Editor.SelectedObject);
                    if (obj is not null)
                    {
                        _instances.UndoRedoServices.PushState();
                        obj.Flipped = !obj.Flipped;
                        _mapFile.OnModified();
                        IsModified = true;
                    }
                }

                EditorViewModel.Redraw();
            });

            EditorViewModel = services.Create<EditorViewModel>();

            if (string.IsNullOrWhiteSpace(_editorData.RecentMapPath) || 
                !LoadMap(_editorData.RecentMapPath))
            {
                New().ConfigureAwait(true);
            }

            TilesetSelectorViewModel = services.Create<TilesetSelectorViewModel>();
            TilesetsContainer = instances.TilesetsContainer;

            Menu = GenerateMenu();
        }

        private bool CanRun() => !_isRunningGame && EditorRunner.RunAssembly != null;

        private async Task Run()
        {
            if (_isRunningGame)
                return;

            if (EditorRunner.RunAssembly is null)
                return;
            
            try
            {
                _isRunningGame = true;
                RunCommand.RaiseCanExecuteChanged();

                await Save();

                if (_filePath is null || MapFile.IsModified)
                {
                    return;
                }

                var path = _filePath;

                var execPath = EditorRunner.RunAssembly.Location;
                
                if (execPath.EndsWith(".dll"))
                {
#if WINDOWS
                    execPath = execPath.Replace(".dll", ".exe");
#else
                    execPath = execPath.Replace(".dll", "");
#endif
                }

                var process = Process.Start(execPath, $"-r:{path}");

                if (process != null)
                {
                    await process.WaitForExitAsync();
                }
            }
            finally
            {
                _isRunningGame = false;
                RunCommand.RaiseCanExecuteChanged();
            }
        }

        private void MapOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IsModified = MapFile?.IsModified ?? false;
        }
        
        private void UpdateTitle()
        {
            string append = MapFile.IsModified ? "*" : "";
            var name = _instances.Template.Name;
            if (_filePath == null)
            {
                Title = $"{name} - New Map" + append;
            }
            else
            {
                Title = $"{name} - " + Path.GetFileNameWithoutExtension(_filePath) + append;
            }
        }
    
        private MenuItemModel[] GenerateMenu()
        {
            return new[]
            {
                new MenuItemModel("File", new[]
                {
                    new MenuItemModel("New", NewCommand),
                    new MenuItemModel("Open", OpenCommand),
                    new MenuItemModel("Save", SaveCommand),
                    new MenuItemModel("Save As...", SaveAsCommand),
                }),
                new MenuItemModel("Edit", new[]
                {
                    new MenuItemModel("Undo", null),
                    new MenuItemModel("Redo", null)
                }),
                new MenuItemModel("Window", null)
            };
        }
        
        private async Task Open()
        {
            try
            {
                var path = await _fileService.ChooseFile(true, "Open Map File", "Map File (.map)");

                if (path == null)
                    return;

                if (await CheckIfSave() == false)
                {
                    return;
                }

                LoadMap(path);
            }
            catch (OperationCanceledException)
            {

            }
        }

        private async Task New()
        {
            if (!await CheckIfSave())
                return;

            var template = _instances.Template;

            var mapFile = new MapFile(template.Guid, template.TileSize, template.TileSets.ToArray());
        
            foreach (var layer in template.Layers)
            {
                MapLayer mapLayer;
                if (layer.Id.Equals(LayerDescription.MainLayerId, StringComparison.InvariantCultureIgnoreCase))
                {
                    mapLayer = new MainLayer(layer.Size.Width, layer.Size.Height, template);
                }
                else
                {
                    mapLayer = new MapLayer(layer.Id, layer.Size.Width, layer.Size.Height, template);
                }

                mapLayer.Name = layer.Name;
                mapLayer.HorizontalSpeed = layer.HorizontalSpeed;
                mapLayer.VerticalSpeed = layer.VerticalSpeed;
                mapLayer.Depth = layer.Depth;
                mapLayer.TintColor = layer.Tint;
                mapLayer.FogColor = layer.Fog;
                
                mapFile.Layers.Add(mapLayer);
                
            }

            _editorData.SelectedLayer = LayerDescription.MainLayerId;
            _editorData.RecentMapPath = null;
            _editorData.RequestSave();

            _filePath = null;
            
            MapFile = mapFile;
            IsModified = false;
            
            UpdateTitle();
        }

        private async Task<bool> CheckIfSave()
        {
            if (IsModified)
            {
                var result = await _windowService.ShowMessageBox("Save changes?", "Do you want to save changes?",
                    MessageBoxType.YesNoCancel);

                if (!result.HasValue)
                {
                    return false;
                }

                if (result.Value)
                {
                    await Save();
                    return !IsModified;
                }
            }

            return true;
        }

        private async Task SaveAs()
        {
            try
            {
                var path = await _fileService.ChooseFile(false, "Save Map File", "Map File (.map)");
                _filePath = path;
                await Save();
            }
            catch (OperationCanceledException)
            {

            }
        }
        
        private bool LoadMap(string path)
        {
            try
            {
                using var stream = File.Open(path, FileMode.Open);
                var mapFile = MapFile.FromStream(stream, _instances.Template,
                    _instances.TilesetsContainer.TileSets.Select(o => o.Path).ToArray());

                if (mapFile.TemplateId == _instances.Template.Guid)
                {
                    _filePath = path;
                    MapFile = mapFile;
                    IsModified = UpdateLayers();

                    _editorData.RecentMapPath = path;
                    _editorData.RequestSave();

                    return true;
                }
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }

            return false;
        }

        private bool UpdateLayers()
        {
            var mapFile = MapFile;
            var newLayers = new List<MapLayer>();
            
            for (var idx =0; idx < _instances.Template.Layers.Length; ++idx)
            {
                var layer = _instances.Template.Layers[idx];
                
                var mapLayer = mapFile.Layers.FirstOrDefault(o => string.Equals(o.Name, layer.Name, StringComparison.InvariantCultureIgnoreCase));
                if (mapLayer is null)
                {
                    mapLayer = new MapLayer(layer.Id, layer.Size.Width, layer.Size.Height, _instances.Template)
                    {
                        Name = layer.Name,
                        HorizontalSpeed = layer.HorizontalSpeed,
                        VerticalSpeed = layer.VerticalSpeed,
                        Depth = layer.Depth,
                        TintColor = layer.Tint,
                        FogColor = layer.Fog
                    };
                }

                mapLayer.Id = layer.Id;
                newLayers.Add(mapLayer);
            }

            var same = mapFile.Layers.Count == newLayers.Count;

            if (same)
            {
                for (var idx = 0; idx < mapFile.Layers.Count; ++idx)
                {
                    if ( mapFile.Layers[idx].Id != newLayers[idx].Id)
                    {
                        same = false;
                        break;
                    }
                }
            }

            if (!same)
            {
                mapFile.Layers.Clear();
                foreach (var layer in newLayers)
                {
                    mapFile.Layers.Add(layer);
                }

                return true;
            }

            return false;
        }

        private async Task Save()
        {
            if (_filePath is null)
            {
                await SaveAs();
                return;
            }
        
            using (var stream = File.Open(_filePath, FileMode.Create))
            {
                //using var gzipStream = new GZipStream(stream, CompressionLevel.SmallestSize);
                MapFile.Save(stream);
            }
        }

        private bool _forceClose;
        private bool _isRunningGame;

        public bool CanClose()
        {
            if (_forceClose)
                return true;
            
            TryClose();
            return false;
        }

        private async void TryClose()
        {
            var canClose = await CheckIfSave();

            if (canClose)
            {
                _forceClose = true;
                _windowService.CloseMainWindow();
            }
        }
    }
}