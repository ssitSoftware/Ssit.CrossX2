using System.Reflection;
using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.IoC;
using Ssit.CrossX2.Framework.IoC.Impl;
using Ssit.CrossX2.Framework.UI.Common.Pages;
using Ssit.CrossX2.Framework.UI.Handlers;
using Ssit.CrossX2.Framework.UI.Handlers.Markdown;
using Ssit.CrossX2.Framework.UI.Internal;
using Ssit.CrossX2.Framework.UI.Services;
using Ssit.CrossX2.Framework.UI.Views;
using Ssit.CrossX2.Framework.UI.Views.Markdown;
using Button = Ssit.CrossX2.Framework.UI.Views.Button;
using ImageView = Ssit.CrossX2.Framework.UI.Views.ImageView;
using ScrollView = Ssit.CrossX2.Framework.UI.Views.ScrollView;

namespace Ssit.CrossX2.Framework.UI;

internal class UiAppBuilder(IIoCContainer container, IRenderHost renderHost) : IUiAppBuilder
{
    private InitializeServicesDelegate _initializeServicesDelegate;
    private MapHandlersDelegate _mapHandlersDelegate;
    private MapNavigationDelegate _mapNavigationDelegate;
    private AppInitializationDelegate _appInitializationDelegate;
    
    private ColorWrapper _backgroundColor;
    private Type[] _styleTypes;

    private Action<INavigation> _firstNavigation;
    private readonly List<Assembly> _autoScanAssembiles = new();

    public IUiAppBuilder WithServiceRegistrar(InitializeServicesDelegate initializeServicesDelegate)
    {
        _initializeServicesDelegate = initializeServicesDelegate;
        return this;
    }

    public IUiAppBuilder WithHandlersMapping(MapHandlersDelegate mapHandlers)
    {
        _mapHandlersDelegate = mapHandlers;
        return this;
    }

    public IUiAppBuilder WithNavigationMapping(MapNavigationDelegate mapNavigation)
    {
        _mapNavigationDelegate = mapNavigation;
        return this;
    }

    public IUiAppBuilder WithAutoNavigationMapping(Assembly assembly)
    {
        _autoScanAssembiles.Add(assembly);
        return this;
    }

    public IUiAppBuilder WithStyles(params Type[] types)
    {
        _styleTypes = types.ToArray();
        return this;
    }
    
    public IUiAppBuilder WithBackgroundColor(ColorWrapper color)
    {
        _backgroundColor = color;
        return this;
    }

    public IUiAppBuilder WithFirstNavigation<TViewModel>(object parameter = null) where TViewModel : class
    {
        _firstNavigation = nav => nav.ClearNavigateTo<TViewModel>(parameter);
        return this;
    }

    public IUiAppBuilder WithUiAppInitialization(AppInitializationDelegate appInitializationDelegate)
    {
        _appInitializationDelegate = appInitializationDelegate;
        return this;
    }

    public IAppComponent Build()
    {
        var map = new NavigationMap();
        var handlers = new HandlerMapper();

        var builder = new IoCContainerBuilder();
        builder.WithParent(container);

        builder
            .WithInstance(map)
            .WithSingleton<INavigation, Navigation>()
            .WithSingleton<IHandlerMapper, FullHandlerMapper>()
            .WithSingleton<IUiServices, UiServices>()
            .WithSingleton<IUiActionDispatcher, UiActionDispatcher>()
            .WithSingleton<IUiSounds, UiSoundsContainer>()
            .WithSingleton<IUiApp, UiApp>()
            .WithInstance<IInputCoordinateSystem>(new InputCoordinateSystem(renderHost))
            .WithSingleton<PageInputContext, PageInputContext>();

        _initializeServicesDelegate?.Invoke(builder);
        _mapHandlersDelegate?.Invoke(handlers);
        
        _autoScanAssembiles?.ForEach(x => map.AutoScan(x));
        _mapNavigationDelegate?.Invoke(map);
        
        var services = builder.Build();
        var navigation = services.Get<INavigation>();
        var handlerMapper = (FullHandlerMapper)services.Get<IHandlerMapper>();

        MapHandlers(handlerMapper);
        handlerMapper.AddMappings(handlers);

        var app = (UiApp)services.Get<IUiApp>();
        app.Initialize((Navigation)navigation);

        if (_styleTypes != null)
        {
            app.LoadStyles(_styleTypes);
        }

        _appInitializationDelegate?.Invoke(app);
        _firstNavigation?.Invoke(navigation);
        
        return services.IoCConstruct<UiAppComponent>(new UiAppComponent.Parameters
        {
            UiApp = app,
            BackgroundColor = _backgroundColor
        });
    }

    private static void MapHandlers(IHandlerMapper handlerMapper)
    {
        handlerMapper
            .AddMapping<Container, ContainerHandler>()
            .AddMapping<Background, BackgroundHandler>()
            .AddMapping<Frame, FrameHandler>()
            .AddMapping<Label, LabelHandler<Label>>()
            .AddMapping<BlinkingLabel, BlinkingLabelHandler<BlinkingLabel>>()
            .AddMapping<LabelButton, LabelButtonHandler<LabelButton>>()
            .AddMapping<LabelButtonEx, LabelButtonExHandler>()
            .AddMapping<LabelRadio, LabelRadioHandler<LabelRadio>>()
            .AddMapping<Button, ButtonHandler>()
            .AddMapping<VerticalStack, VerticalStackHandler<VerticalStack>>()
            .AddMapping<ImageView, ImageViewHandler>()
            .AddMapping<ScrollView, ScrollViewHandler<ScrollView>>()
            .AddMapping<VirtualButton, VirtualButtonHandler>()
            .AddMapping<IconCheckBox, IconCheckBoxHandler<IconCheckBox>>()
            .AddMapping<SpriteView, SpriteViewHandler>()
            .AddMapping<MarkdownView, MarkdownViewHandler<MarkdownView>>()
            .AddMapping<HorizontalSlider, HorizontalSliderHandler<HorizontalSlider>>()
            .AddMapping<FocusableContainer, FocusableContainerHandler>()
            .AddMapping<TextInput, TextInputHandler>();
    }
}