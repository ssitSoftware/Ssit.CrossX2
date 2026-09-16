namespace CrossX2.Services;

public interface IActionScheduler
{
    void Schedule(Action action);
    void ExecuteOnMainThread(Action action);
}

