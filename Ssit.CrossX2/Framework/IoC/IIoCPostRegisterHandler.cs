namespace Ssit.CrossX2.Framework.IoC;

/// <summary>
/// Receives a callback after all services have been registered in the IoC container.
/// </summary>
public interface IIoCPostRegisterHandler
{
    void OnAllServicesRegistered();
}
