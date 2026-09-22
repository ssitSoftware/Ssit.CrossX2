using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace Ssit.CrossX2.Editor.Controls
{
    public partial class SelectionToggleButton : UserControl
    {
        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<SelectionToggleButton, string>(nameof(Text),
                defaultBindingMode: BindingMode.TwoWay);
    
        public static readonly StyledProperty<string> IconProperty =
            AvaloniaProperty.Register<SelectionToggleButton, string>(nameof(Icon));
    
        public static readonly StyledProperty<object> ValueProperty =
            AvaloniaProperty.Register<SelectionToggleButton, object>(nameof(Value));
    
        public static readonly StyledProperty<object> SelectedValueProperty =
            AvaloniaProperty.Register<SelectionToggleButton, object>(nameof(SelectedValue),
                defaultBindingMode: BindingMode.TwoWay);

        public SelectionToggleButton()
        {
            InitializeComponent();
        }

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
    
        public string Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
    
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
            Btn.IsChecked = true;
            SelectedValue = Value;
        }
    }
}