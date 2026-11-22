using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using Ssit.CrossX2.Editor.Models;
using Ssit.CrossX2.Editor.Service;
using Ssit.CrossX2.Editor.Tools;
using Ssit.CrossX2.Framework.Games.Map;
using Ssit.CrossX2.Framework.Utils;

namespace Ssit.CrossX2.Editor.ViewModels;

/// <summary>A prefab file in the library: name, preview and short info, read without loading its layers.</summary>
public class PrefabItem
{
    public string Name { get; init; }
    public string Path { get; init; }
    public Bitmap Preview { get; init; }
    public string Info { get; init; }
}

/// <summary>
/// Lists the prefabs of the remembered prefab folder with their previews. Picking one loads it into the
/// <see cref="PlacePrefabTool"/> and makes that the current tool.
/// </summary>
public class PrefabLibraryViewModel : BindableModel
{
    private readonly IEditorInstances _instances;
    private readonly IFileService _fileService;
    private readonly IWindowService _windowService;
    private readonly EditorData _editorData;

    private bool _clearingSelection;

    public ObservableCollection<PrefabItem> Prefabs { get; } = new();

    public PrefabItem SelectedPrefab
    {
        get;
        set
        {
            if (SetField(ref field, value) && value is not null && !_clearingSelection)
            {
                PickPrefab(value);
            }
        }
    }

    public string Folder => _editorData.PrefabFolder;

    public string FolderName => string.IsNullOrWhiteSpace(Folder) ? "No prefab folder" : System.IO.Path.GetFileName(Folder);

    public bool IsEmpty => Prefabs.Count == 0;

    public string EmptyMessage
    {
        get;
        private set => SetField(ref field, value);
    }

    public ICommand RefreshCommand { get; }
    public IAsyncRelayCommand ChooseFolderCommand { get; }

    public PrefabLibraryViewModel(IEditorInstances instances, IFileService fileService, IWindowService windowService,
        EditorData editorData)
    {
        _instances = instances;
        _fileService = fileService;
        _windowService = windowService;
        _editorData = editorData;

        RefreshCommand = new RelayCommand(Refresh);
        ChooseFolderCommand = new AsyncRelayCommand(ChooseFolder);

        // Picking the same prefab again after using another tool should work, so drop the selection then.
        _instances.Tools.PropertyChanged += (_, _) =>
        {
            if (_instances.Tools.Current is not PlacePrefabTool && SelectedPrefab is not null)
            {
                _clearingSelection = true;
                SelectedPrefab = null;
                _clearingSelection = false;
            }
        };

        Refresh();
    }

    /// <summary>Reads the prefab folder again (also called after a prefab is extracted).</summary>
    public void Refresh()
    {
        var selectedPath = SelectedPrefab?.Path;

        foreach (var item in Prefabs)
        {
            item.Preview?.Dispose();
        }

        _clearingSelection = true;
        SelectedPrefab = null;
        Prefabs.Clear();
        _clearingSelection = false;

        var folder = Folder;

        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            EmptyMessage = "Choose the prefab folder, or extract a prefab with the region selection tool.";
            NotifyFolderChanged();
            return;
        }

        var files = Directory.EnumerateFiles(folder, "*" + PrefabFile.Extension)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var item = ReadItem(file);
            if (item is not null)
            {
                Prefabs.Add(item);
            }
        }

        EmptyMessage = "No prefabs in this folder yet.";
        NotifyFolderChanged();

        if (selectedPath is not null)
        {
            _clearingSelection = true;
            SelectedPrefab = Prefabs.FirstOrDefault(p => p.Path == selectedPath);
            _clearingSelection = false;
        }
    }

    private PrefabItem ReadItem(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            var info = PrefabFile.ReadInfo(stream);

            // Prefabs of other games' templates can't be placed here.
            if (info.TemplateId != _instances.Template.Guid) return null;

            return new PrefabItem
            {
                Name = System.IO.Path.GetFileNameWithoutExtension(path),
                Path = path,
                Preview = info.PreviewPng is null ? null : new Bitmap(new MemoryStream(info.PreviewPng)),
                Info = $"{info.Width} x {info.Height}" + (info.IsLayerBound ? ", several layers" : "")
            };
        }
        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private void NotifyFolderChanged()
    {
        OnPropertyChanged(nameof(Folder));
        OnPropertyChanged(nameof(FolderName));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private async Task ChooseFolder()
    {
        string folder;
        try
        {
            folder = await _fileService.ChooseFolder("Prefab Folder", Folder);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        _editorData.PrefabFolder = folder;
        _editorData.RequestSave();

        Refresh();
    }

    private async void PickPrefab(PrefabItem item)
    {
        PrefabFile prefab;

        try
        {
            using var stream = File.OpenRead(item.Path);
            prefab = PrefabFile.FromStream(stream, _instances.Template,
                _instances.TilesetsContainer.TileSets.Select(t => t.Path).ToArray());
        }
        catch (Exception e)
        {
            // Unreadable file, or it uses objects the game template doesn't have any more.
            await _windowService.ShowMessageBox("Prefab not loaded", $"{item.Name}: {e.Message}", MessageBoxType.Ok);
            return;
        }

        var tool = _instances.Tools.GetTool<PlacePrefabTool>();
        tool.PrefabName = item.Name;
        tool.Prefab = prefab;

        _instances.Tools.Current = tool;
        _instances.Editor?.Redraw();
    }
}
