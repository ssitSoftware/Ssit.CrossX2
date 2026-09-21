using System.Collections.Concurrent;

namespace Ssit.CrossX2.Framework.Services.Internal;

internal class ActionScheduler: IInternalActionScheduler
{
    private readonly ConcurrentQueue<Action> _actionQueue = new();
    private readonly int _mainThreadId = Environment.CurrentManagedThreadId;

    public void Schedule(Action action) => _actionQueue.Enqueue(action);

    public void ExecuteOnMainThread(Action action)
    {
        var currId = Thread.CurrentThread.ManagedThreadId;

        if (currId == _mainThreadId) action.Invoke();
        else Schedule(action);
    }
    
    public void Process()
    {
        while (_actionQueue.TryDequeue(out var action))
        {
            action();
        }
    }
}