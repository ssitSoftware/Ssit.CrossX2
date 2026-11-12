namespace Ssit.CrossX2.Framework.Services;

/// <summary>
/// Schedules actions for execution on the application main thread.
/// </summary>
public interface IActionScheduler
{
    void Schedule(Action action);
    void ExecuteOnMainThread(Action action);
    bool IsMainThread { get; }
}
