using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Ssit.CrossX2.Editor.Input;
using Ssit.CrossX2.Editor.Service;
using Ssit.CrossX2.Framework.Games.Map;
using Ssit.CrossX2.Framework.Utils;

namespace Ssit.CrossX2.Editor.ViewModels;

public class ResizeLayerDialogViewModel: BindableModel, IDialog
{
    private readonly MapLayer _layer;
    private readonly IEditorInstances _instances;

    public event Action RequestClose;
    
    public string Title { get; }

    public MapAlign Align
    {
        get;
        set => SetField(ref field, value);
    }

    public MapAlign[] Aligns { get; }

    public int Width
    {
        get;
        set => SetField(ref field, value);
    }

    public int Height
    {
        get;
        set => SetField(ref field, value);
    }

    public ICommand CloseCommand { get; }
    public ICommand ApplyCommand { get; }

    public ResizeLayerDialogViewModel(MapLayer layer, IEditorInstances instances)
    {
        _layer = layer;
        _instances = instances;
        Title = $"Resize {layer.Name} layer";

        Width = _layer.Width;
        Height = _layer.Height;

        Aligns = new[]
        {
            MapAlign.Left, MapAlign.Center, MapAlign.Right,
            MapAlign.VCenter, MapAlign.VCenterCenter, MapAlign.VCenterRight,
            MapAlign.Bottom, MapAlign.BottomCenter, MapAlign.BottomRight
        };
        Align = MapAlign.Bottom;

        CloseCommand = new RelayCommand(() => RequestClose?.Invoke());
        ApplyCommand = new RelayCommand(Apply);
    }
    
    private void Apply()
    {
        _instances.UndoRedoServices.PushState();
        _layer.Resize(Width, Height, Align);
        RequestClose?.Invoke();
        _instances.Map.OnModified();
    }
}