using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Ssit.CrossX2.Editor.Controls;
using Ssit.CrossX2.Editor.ViewModels;

namespace Ssit.CrossX2.Editor.Panels;

public partial class ImagesSelector : UserControl
{
    public static readonly StyledProperty<ImageSelectorViewModel> ViewModelProperty =
        AvaloniaProperty.Register<SelectionToggleButton, ImageSelectorViewModel>(nameof(ViewModel),
            defaultBindingMode: BindingMode.TwoWay);
    
    public ImageSelectorViewModel ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }
    
    public ImagesSelector()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        ViewModel = DataContext as ImageSelectorViewModel;
    }
}