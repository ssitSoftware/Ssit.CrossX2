using Ssit.CrossX2.IoC;

namespace Ssit.CrossX2.UI.Services;

public delegate void InitializeUiDelegate(IIoCContainerBuilder builder, INavigationMap navigationMap, IHandlerMapper mapper);