using Ssit.CrossX2.Framework.IoC;

namespace Ssit.CrossX2.Framework.UI.Services;

public delegate void InitializeUiDelegate(IIoCContainerBuilder builder, INavigationMap navigationMap, IHandlerMapper mapper);