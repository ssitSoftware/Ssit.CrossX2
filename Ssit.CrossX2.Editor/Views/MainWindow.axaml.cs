using System;
using Avalonia.Controls;
using Ssit.CrossX2.Editor.Helpers;
using Ssit.CrossX2.Editor.ViewModels;
using Ssit.CrossX2.Framework.Games.Template;

namespace Ssit.CrossX2.Editor.Views
{
    public partial class MainWindow : Window
    {
        private readonly IGameTemplate _gameTemplate;
    
        public MainWindow(IGameTemplate gameTemplate)
        {
            _gameTemplate = gameTemplate;
            InitializeComponent();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
        
            var vm = DataContext as MainViewModel;
            if (vm is null) return;
        
            var menu = MenuGenerator.GenerateNativeMenu(vm.Menu);
            NativeMenu.SetMenu(this, menu);

            var zoom = _gameTemplate.TilesetPanelZoom;
            vm.TilesetSelectorViewModel.Zoom.SetZoom(zoom);
        }

        private void Window_OnClosing(object sender, WindowClosingEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm is null) return;

            e.Cancel = !vm.CanClose();
        }
    }
}