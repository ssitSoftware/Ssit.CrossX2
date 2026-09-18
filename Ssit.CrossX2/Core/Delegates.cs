using Ssit.CrossX2.IoC;

namespace Ssit.CrossX2.Core;

public delegate void InitializeServicesDelegate(IIoCContainerBuilder builder);
public delegate void InitializeAppDelegate(IIoCContainer container);