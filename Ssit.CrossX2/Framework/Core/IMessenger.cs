namespace Ssit.CrossX2.Framework.Core;

public interface IMessenger
{
    event Action<object> Message;
    void PostMessage(object message);
}