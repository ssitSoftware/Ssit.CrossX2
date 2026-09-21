using Ssit.CrossX2.Framework.IoC.Impl;

namespace Ssit.CrossX2.Framework.IoC;

public static class IoCAgent
{
    public static IIoCContainerBuilder NewBuilder() => new IoCContainerBuilder();
}