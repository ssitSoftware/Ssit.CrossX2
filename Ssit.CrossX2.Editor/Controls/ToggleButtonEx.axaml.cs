using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace Ssit.CrossX2.Editor.Controls;

public partial class ToggleButtonEx : UserControl
{
    public static readonly StyledProperty<object> ValueProperty =
        AvaloniaProperty.Register<ToggleButtonEx, object>(nameof(Value));
    
    public static readonly StyledProperty<object> SelectedValueProperty =
        AvaloniaProperty.Register<ToggleButtonEx, object>(nameof(SelectedValue),
            defaultBindingMode: BindingMode.TwoWay);
    
    public static readonly StyledProperty<AvaloniaObject> ViewProperty =
        AvaloniaProperty.Register<ToggleButtonEx, AvaloniaObject>(nameof(View));
    
    public object Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
    
    public object SelectedValue
    {
        get => GetValue(SelectedValueProperty);
        set => SetValue(SelectedValueProperty, value);
    }
    
    public AvaloniaObject View
    {
        get => GetValue(ViewProperty);
        set => SetValue(ViewProperty, value);
    }
    
    public ToggleButtonEx()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty || change.Property == SelectedValueProperty)
        {
            Btn.IsChecked = Value?.Equals(SelectedValue) ?? false;
        }
    }

    private void Button_OnClick(object sender, RoutedEventArgs e)
    {
        SelectedValue = null;
        SelectedValue = Value;
        Btn.IsChecked = true;
    }
}