using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace Ssit.CrossX2.Editor.Controls;

public partial class NumericInput : UserControl
{
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<NumericInput, double>(nameof(Minimum), -10000);
    
    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<NumericInput, double>(nameof(Maximum), 10000);
    
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<NumericInput, double>(nameof(Value),
            defaultBindingMode: BindingMode.TwoWay);
    
    public static readonly StyledProperty<double> StepProperty =
        AvaloniaProperty.Register<NumericInput, double>(nameof(Step));
    
    public static readonly StyledProperty<ICommand> FocusLostCommandProperty =
        AvaloniaProperty.Register<NumericInput, ICommand>(nameof(FocusLostCommand));
    
    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
    
    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }
    
    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Step
    {
        get => GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }
    
    public ICommand FocusLostCommand
    {
        get => GetValue(FocusLostCommandProperty);
        set => SetValue(FocusLostCommandProperty, value);
    }

    public NumericInput()
    {
        InitializeComponent();
    }

    private void Slider_OnLostFocus(object _, RoutedEventArgs _1)
    {
        FocusLostCommand?.Execute(null);
    }
}