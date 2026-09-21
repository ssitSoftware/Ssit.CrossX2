namespace Ssit.CrossX2.Framework.Services;

public interface IActionScheduler
{
    void Schedule(Action action);
    void ExecuteOnMainThread(Action action);
}

