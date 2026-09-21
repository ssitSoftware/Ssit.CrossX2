using System.Reflection;
using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.UI;

public interface IUiAppBuilder
{
    IUiAppBuilder WithServiceRegistrar(InitializeServicesDelegate initializeServicesDelegate);
    IUiAppBuilder WithHandlersMapping(MapHandlersDelegate mapHandlers);
    IUiAppBuilder WithNavigationMapping(MapNavigationDelegate mapNavigationMap);
    IUiAppBuilder WithAutoNavigationMapping(Assembly assembly);
    IUiAppBuilder WithStyles(params Type[] types);
    IUiAppBuilder WithBackgroundColor(ColorWrapper color);
    IUiAppBuilder WithFirstNavigation<TViewModel>(object parameter = null) where TViewModel : class;
    IUiAppBuilder WithUiAppInitialization(AppInitializationDelegate appInitializationDelegate);

    IAppComponent Build();
}