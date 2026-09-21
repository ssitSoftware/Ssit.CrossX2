using Ssit.CrossX2.Framework.IoC;

namespace Ssit.CrossX2.Framework.Core;

public delegate void InitializeServicesDelegate(IIoCContainerBuilder builder);
public delegate void InitializeAppDelegate(IIoCContainer container);