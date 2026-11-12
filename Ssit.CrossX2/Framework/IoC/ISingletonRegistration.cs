namespace Ssit.CrossX2.Framework.IoC;

/// <summary>
/// Fluent builder stage for configuring a singleton registration.
/// </summary>
public interface ISingletonRegistration<out TImplementation>: IIoCContainerBuilder where TImplementation : class
{
    ISingletonRegistration<TImplementation> OnCreated(Action<TImplementation> action);
    ISingletonRegistration<TImplementation> OnCreated(Action<TImplementation, IIoCContainer> action);
    ISingletonRegistration<TImplementation> AsImplementedInterfaces();
}
