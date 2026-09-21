namespace Ssit.CrossX2.Framework.Content;

public interface IInstanceCountingResource: IDisposable
{
    Action<Guid> AddUser {set;}
    Action<Guid> RemoveUser {set;}
}