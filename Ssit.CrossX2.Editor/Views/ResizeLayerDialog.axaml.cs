using System;
using Avalonia.Controls;
using Ssit.CrossX2.Editor.Helpers;

namespace Ssit.CrossX2.Editor.Views;

public partial class ResizeLayerDialog : Window
{
    public ResizeLayerDialog()
    {
        InitializeComponent();
    }
    
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        WindowHelpers.FitDialog(this, Pnl);
    }
}