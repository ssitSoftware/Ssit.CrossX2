namespace Ssit.CrossX2.Framework.Content;

/// <summary>
/// Resource that tracks handle users through add and remove callbacks.
/// </summary>
public interface IInstanceCountingResource: IDisposable
{
    Action<Guid> AddUser {set;}
    Action<Guid> RemoveUser {set;}
}
