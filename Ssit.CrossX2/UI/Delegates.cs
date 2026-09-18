using Ssit.CrossX2.IoC;
using Ssit.CrossX2.UI.Services;

namespace Ssit.CrossX2.UI;

public delegate void MapHandlersDelegate(IHandlerMapper map);
public delegate void MapNavigationDelegate(INavigationMap map);
public delegate void AppInitializationDelegate(IUiApp app);