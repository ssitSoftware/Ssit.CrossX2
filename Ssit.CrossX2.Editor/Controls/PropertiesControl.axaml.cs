using Avalonia.Controls;
using Avalonia.Interactivity;
using Ssit.CrossX2.Editor.Models.Parameters;

namespace Ssit.CrossX2.Editor.Controls;

public partial class PropertiesControl : UserControl
{
    public PropertiesControl()
    {
        InitializeComponent();
    }

    private void InputElement_OnLostFocus(object sender, RoutedEventArgs e)
    {
        ((sender as TextBox)?.DataContext as ParameterStringModel)?.FocusLostCommand?.Execute(null);
    }
}