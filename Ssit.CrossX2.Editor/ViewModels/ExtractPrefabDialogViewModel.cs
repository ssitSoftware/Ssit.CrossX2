using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using Ssit.CrossX2.Editor.Input;
using Ssit.CrossX2.Editor.Models;
using Ssit.CrossX2.Editor.Service;
using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.Games.Map;
using Ssit.CrossX2.Framework.Utils;

namespace Ssit.CrossX2.Editor.ViewModels;

/// <summary>
/// Extracts the region selection as a prefab: choose the current layer or all layers with the same speed,
/// tiles and objects or tiles only, check the preview, save to the (remembered) prefab folder.
/// Both choices start from, and are remembered as, the defaults in <see cref="EditorData"/>.
/// </summary>
public class ExtractPrefabDialogViewModel : BindableModel, IDialog
{
    public class Parameters(Rectangle region, MapLayer layer)
    {
        public Rectangle Region { get; } = region;
        public MapLayer Layer { get; } = layer;
    }

    private const int PreviewSize = 512;

    private readonly Parameters _parameters;
    private readonly IFileService _fileService;
    private readonly IWindowService _windowService;
    private readonly EditorData _editorData;
    private readonly PrefabBuilder _builder;

    private PrefabFile _prefab;
    private bool _allMatchingLayers;
    private bool _tilesOnly;

    public event Action RequestClose;

    public string Title => "Extract prefab";

    public string CurrentLayerLabel { get; }
    public string MatchingLayersLabel { get; }

    /// <summary>False when no other layer has the same speed as the current one.</summary>
    public bool CanUseMatchingLayers { get; }

    public bool CurrentLayerOnly
    {
        get => !_allMatchingLayers;
        set
        {
            if (value) SetAllMatchingLayers(false);
        }
    }

    public bool AllMatchingLayers
    {
        get => _allMatchingLayers;
        set
        {
            if (value) SetAllMatchingLayers(true);
        }
    }

    public bool TilesAndObjects
    {
        get => !_tilesOnly;
        set
        {
            if (value) SetTilesOnly(false);
        }
    }

    public bool TilesOnly
    {
        get => _tilesOnly;
        set
        {
            if (value) SetTilesOnly(true);
        }
    }

    public Bitmap Preview
    {
        get;
        private set => SetField(ref field, value);
    }

    public string Info
    {
        get;
        private set => SetField(ref field, value);
    }

    public bool CanSave
    {
        get;
        private set => SetField(ref field, value);
    }

    public ICommand CancelCommand { get; }
    public IAsyncRelayCommand SaveCommand { get; }

    public ExtractPrefabDialogViewModel(Parameters parameters, IEditorInstances instances, IFileService fileService,
        IWindowService windowService, EditorData editorData)
    {
        _parameters = parameters;
        _fileService = fileService;
        _windowService = windowService;
        _editorData = editorData;
        _builder = new PrefabBuilder(instances);

        var matching = _builder.GetMatchingLayers(parameters.Layer);

        CurrentLayerLabel = $"Current layer only ({parameters.Layer.Name})";
        MatchingLayersLabel = $"All layers with the same speed ({string.Join(", ", matching.Select(l => l.Name))})";
        CanUseMatchingLayers = matching.Count > 1;

        CancelCommand = new RelayCommand(() => RequestClose?.Invoke());
        SaveCommand = new AsyncRelayCommand(Save, () => CanSave);

        // Start from the remembered choices; "all layers" falls back to the current layer when nothing matches.
        _tilesOnly = editorData.PrefabTilesOnly;
        _allMatchingLayers = editorData.PrefabAllMatchingLayers && CanUseMatchingLayers;

        Rebuild();
    }

    private void SetAllMatchingLayers(bool value)
    {
        if (_allMatchingLayers == value) return;

        _allMatchingLayers = value;
        OnPropertyChanged(nameof(CurrentLayerOnly));
        OnPropertyChanged(nameof(AllMatchingLayers));

        _editorData.PrefabAllMatchingLayers = value;
        _editorData.RequestSave();

        Rebuild();
    }

    private void SetTilesOnly(bool value)
    {
        if (_tilesOnly == value) return;

        _tilesOnly = value;
        OnPropertyChanged(nameof(TilesAndObjects));
        OnPropertyChanged(nameof(TilesOnly));

        _editorData.PrefabTilesOnly = value;
        _editorData.RequestSave();

        Rebuild();
    }

    private void Rebuild()
    {
        _prefab = _builder.Build(_parameters.Region, _parameters.Layer, _allMatchingLayers, includeObjects: !_tilesOnly);

        try
        {
            _prefab.PreviewPng = _builder.RenderPreview(_prefab, PreviewSize);
        }
        catch (Exception)
        {
            // A broken preview shouldn't block saving the prefab.
            _prefab.PreviewPng = null;
        }

        var oldPreview = Preview;
        Preview = _prefab.PreviewPng is null ? null : new Bitmap(new MemoryStream(_prefab.PreviewPng));
        oldPreview?.Dispose();

        Info = CreateInfo(_prefab, _tilesOnly);

        CanSave = _prefab.Layers.Count > 0;
        SaveCommand.NotifyCanExecuteChanged();
    }

    private static string CreateInfo(PrefabFile prefab, bool tilesOnly)
    {
        if (prefab.Layers.Count == 0)
            return tilesOnly
                ? "The region has no tiles on the chosen layers."
                : "The region has no tiles or objects on the chosen layers.";

        var objects = prefab.Layers.Sum(l => l.Objects.Count);
        var size = tilesOnly
            ? $"{prefab.Width} x {prefab.Height} tiles, no objects."
            : $"{prefab.Width} x {prefab.Height} tiles, {objects} object{(objects == 1 ? "" : "s")}.";

        return prefab.IsLayerBound
            ? $"{size} Parts on {prefab.Layers.Count} layers ({string.Join(", ", prefab.Layers.Select(l => l.Name))}): " +
              "placing the prefab puts each part back on its layer."
            : $"{size} Single layer: the prefab can be placed on any layer.";
    }

    private async Task Save()
    {
        if (!CanSave) return;

        string path;
        try
        {
            path = await _fileService.ChooseFile(false, "Save Prefab", "Prefab File (.prefab)",
                _editorData.PrefabFolder, "Prefab");
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (!path.EndsWith(PrefabFile.Extension, StringComparison.OrdinalIgnoreCase))
        {
            path += PrefabFile.Extension;
        }

        try
        {
            using var stream = File.Open(path, FileMode.Create);
            _prefab.Save(stream);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            await _windowService.ShowMessageBox("Prefab not saved", e.Message, MessageBoxType.Ok);
            return;
        }

        _editorData.PrefabFolder = Path.GetDirectoryName(path);
        _editorData.RequestSave();

        RequestClose?.Invoke();

        var preview = Preview;
        Preview = null;
        preview?.Dispose();
    }
}
