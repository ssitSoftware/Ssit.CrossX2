namespace Ssit.CrossX2.Framework.UI.Services;

internal class UiActionDispatcher : IUiActionDispatcher
{
    private readonly object _lock = new();
    
    private readonly List<Action> _actions = new();
    private readonly List<Action> _tempList = new();
    
    public void Dispatch()
    {
        lock (_lock)
        {
            _tempList.Clear();
            _tempList.AddRange(_actions);
            _actions.Clear();
        }

        foreach (var action in _tempList)
        {
            action();
        }
        _tempList.Clear();
    }
    
    public void Enqueue(Action action)
    {
        lock (_lock)
        {
            _actions.Add(action);
        }
    }
}