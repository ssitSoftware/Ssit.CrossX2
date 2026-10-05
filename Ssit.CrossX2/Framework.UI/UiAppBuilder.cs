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
    private List<InitializeServicesDelegate> _initializeServicesDelegates = new();
    private List<MapHandlersDelegate> _mapHandlersDelegates = new();
    private List<MapNavigationDelegate> _mapNavigationDelegates = new();
    private List<AppInitializationDelegate> _appInitializationDelegates = new();
    
    private ColorWrapper _backgroundColor;
    private List<Type> _styleTypes = new();

    private Action<INavigation> _firstNavigation;
    private readonly List<Assembly> _autoScanAssembiles = new();

    public IUiAppBuilder WithServiceRegistrar(InitializeServicesDelegate initializeServicesDelegate)
    {
        _initializeServicesDelegates.Add(initializeServicesDelegate);
        return this;
    }

    public IUiAppBuilder WithHandlersMapping(MapHandlersDelegate mapHandlers)
    {
        _mapHandlersDelegates.Add(mapHandlers);
        return this;
    }

    public IUiAppBuilder WithNavigationMapping(MapNavigationDelegate mapNavigation)
    {
        _mapNavigationDelegates.Add(mapNavigation);
        return this;
    }

    public IUiAppBuilder WithAutoNavigationMapping(Assembly assembly)
    {
        _autoScanAssembiles.Add(assembly);
        return this;
    }

    public IUiAppBuilder WithStyles(params Type[] types)
    {
        _styleTypes.AddRange(types ?? []);
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
        _appInitializationDelegates.Add(appInitializationDelegate);
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
            .WithSingleton<INavigation, Navigation>().As<Navigation>()
            .WithSingleton<IHandlerMapper, FullHandlerMapper>()
            .WithSingleton<IUiServices, UiServices>()
            .WithSingleton<IUiActionDispatcher, UiActionDispatcher>()
            .WithSingleton<IUiSounds, UiSoundsContainer>()
            .WithSingleton<IUiApp, UiApp>()
            .WithInstance<IInputCoordinateSystem>(new InputCoordinateSystem(renderHost))
            .WithSingleton<PageInputContext, PageInputContext>()
            .WithInstance(new UiParameters());

        foreach (var del in _initializeServicesDelegates)
        {
            del.Invoke(builder);
        }

        foreach (var del in _mapHandlersDelegates)
        {
            del.Invoke(handlers);
        }
        
        _autoScanAssembiles?.ForEach(x => map.AutoScan(x));

        foreach (var del in _mapNavigationDelegates)
        {
            del.Invoke(map);
        }
        
        var services = builder.Build();
        var navigation = services.Get<INavigation>();
        var handlerMapper = (FullHandlerMapper)services.Get<IHandlerMapper>();

        MapHandlers(handlerMapper);
        handlerMapper.AddMappings(handlers);

        var app = (UiApp)services.Get<IUiApp>();
        app.Navigation = navigation as Navigation;
        app.InputProcessor = services.IoCConstruct<InputProcessor>();

        if (_styleTypes != null)
        {
            app.LoadStyles(_styleTypes.ToArray());
        }

        foreach (var del in _appInitializationDelegates)
        {
            del.Invoke(app);
        }
        _firstNavigation?.Invoke(navigation);

        var updatables = services.Fetch<IUpdatable>(false).ToArray();
        
        return services.IoCConstruct<UiAppComponent>(new UiAppComponent.Parameters
        {
            UiApp = app,
            BackgroundColor = _backgroundColor,
            Updatables = updatables
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
            .AddMapping<ButtonEx, ButtonExHandler>()
            .AddMapping<VerticalStack, VerticalStackHandler<VerticalStack>>()
            .AddMapping<HorizontalStack, HorizontalStackHandler<HorizontalStack>>()
            .AddMapping<ImageView, ImageViewHandler>()
            .AddMapping<IconView, IconViewHandler>()
            .AddMapping<ScrollView, ScrollViewHandler<ScrollView>>()
            .AddMapping<VirtualButton, VirtualButtonHandler>()
            .AddMapping<IconCheckBox, IconCheckBoxHandler<IconCheckBox>>()
            .AddMapping<SpriteView, SpriteViewHandler>()
            .AddMapping<MarkdownView, MarkdownViewHandler<MarkdownView>>()
            .AddMapping<HorizontalSlider, HorizontalSliderHandler<HorizontalSlider>>()
            .AddMapping<FocusableContainer, FocusableContainerHandler>()
            .AddMapping<TextInput, TextInputHandler>()
            .AddMapping<NinePatchCard, NinePatchCardHandler>();
    }
}