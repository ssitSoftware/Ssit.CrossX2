using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using Projektanker.Icons.Avalonia.MaterialDesign;
using Ssit.CrossX2.Editor.Models;
using Ssit.CrossX2.Editor.Service;
using Ssit.CrossX2.Editor.ViewModels;
using Ssit.CrossX2.Editor.Views;
using Ssit.CrossX2.Framework.Games.Template;

namespace Ssit.CrossX2.Editor
{
    public class App : Application
    {
        private readonly IServices _services = new Services();

        public App(IGameTemplate template)
        {
            _services.Register(template);
            
            IconProvider.Current
                .Register<FontAwesomeIconProvider>()
                .Register<MaterialDesignIconProvider>();
        }
        
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            _services.Register(ApplicationLifetime);
            _services.Register<IFileService, FileService>();
            _services.Register<IWindowService, WindowService>();
        
            var template = _services.Get<IGameTemplate>();
        
            _services.Register<ITilesetsContainer, TilesetsContainer>().Load();
            _services.Register<ISpritesContainer, SpritesContainer>().Load();
            _services.Register<IObjectsContainer, ObjectsContainer>().Load();
            _services.Register<IImagesContainer, ImagesContainer>().Load();
            _services.Register<IEditorBitmapsProvider, EditorBitmapsProvider>();

            _services.Register(EditorData.Load(template.Name + "Editor"));
            
            var editorInstances = _services.Register<IEditorInstances, EdtitorInstances>();
            editorInstances.Tools = _services.Register<IEditorTools, EditorTools>();
            editorInstances.UndoRedoServices = new UndoRedoService(editorInstances);

            Name = template.Name + " Editor";

            Resources["EmptyBackground"] = new Color(255, template.EmptyColor.R, template.EmptyColor.G, template.EmptyColor.B);
            Resources["TilesBackground"] = new Color(255, template.TilesBgColor.R, template.TilesBgColor.G, template.TilesBgColor.B);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow(_services.Get<IGameTemplate>())
                {
                    DataContext = _services.Create<MainViewModel>()
                };
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = _services.Create<MainViewModel>()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}