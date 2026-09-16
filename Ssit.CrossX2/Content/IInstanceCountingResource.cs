namespace CrossX2.Content;

public interface IInstanceCountingResource: IDisposable
{
    Action<Guid> AddUser {set;}
    Action<Guid> RemoveUser {set;}
}