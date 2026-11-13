namespace Ssit.CrossX2.Framework.Games.Logic;

public sealed class StateMachine<TObject>(TObject obj, Action<string> onStateChanged = null)
{
    public bool Debug { get; set; }
    
    private readonly Dictionary<string, State<TObject>> _states = new();
    private State<TObject> _currentState;

    public TObject Object { get; } = obj;

    public string CurrentState { get; private set; }

    public void AddState(string name, State<TObject> state) => _states.Add(name, state);

    public void SetState(string state)
    {
        if (CurrentState == state)
            return;

        if (!_states.TryGetValue(state, out var instance))
            throw new InvalidOperationException();

        _currentState?.Leave(Object);
        _currentState = instance;

        CurrentState = state;
        onStateChanged?.Invoke(state);

        _currentState?.Enter(Object);
        
        if (Debug)
        {
            Console.WriteLine($"StateMachine<{typeof(TObject).Name}>: {state}");
        }
    }

    /// <summary>
    /// Leaves the current state without entering a new one - the next <see cref="SetState"/> always enters its state
    /// (also when it is the same state as before the reset).
    /// </summary>
    public void Reset()
    {
        _currentState?.Leave(Object);
        _currentState = null;
        CurrentState = null;
    }

    public void FixedUpdate(float dt) => _currentState?.FixedUpdate(Object, dt);
}
