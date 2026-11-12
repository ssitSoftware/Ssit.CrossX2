namespace Ssit.CrossX2.Framework.Core;

/// <summary>
/// Publishes application-wide messages to subscribed listeners.
/// </summary>
public interface IMessenger
{
    event Action<object> Message;
    void PostMessage(object message);
}
