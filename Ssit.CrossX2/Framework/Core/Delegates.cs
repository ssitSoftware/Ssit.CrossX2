using Ssit.CrossX2.Framework.IoC;

namespace Ssit.CrossX2.Framework.Core;

/// <summary>
/// Registers additional services before the IoC container is built.
/// </summary>
public delegate void InitializeServicesDelegate(IIoCContainerBuilder builder);

/// <summary>
/// Configures the application after the IoC container has been built.
/// </summary>
public delegate void InitializeAppDelegate(IIoCContainer container);
