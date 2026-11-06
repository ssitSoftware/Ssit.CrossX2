namespace Ssit.CrossX2.Framework.Games.Logic;

public sealed class StateMachine<TObject>(TObject obj, Action<string> onStateChanged = null)
{
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
    }

    public void FixedUpdate(float dt) => _currentState?.FixedUpdate(Object, dt);
}
