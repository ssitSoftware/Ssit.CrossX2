using Ssit.CrossX2.Framework.IoC.Impl;

namespace Ssit.CrossX2.Framework.IoC;

/// <summary>
/// Entry point for creating new IoC container builders.
/// </summary>
public static class IoCAgent
{
    public static IIoCContainerBuilder NewBuilder() => new IoCContainerBuilder();
}
