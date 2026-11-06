namespace Ssit.CrossX2.Framework.Games.Logic;

public sealed class StateMachine(Action<string> onStateChanged = null)
{
    private readonly Dictionary<string, State> _states = new();
    private State _currentState;

    public string CurrentState { get; private set; }

    public void AddState(string name, State state) => _states.Add(name, state);

    public void SetState(string state)
    {
        if (CurrentState == state)
            return;

        if (!_states.TryGetValue(state, out var instance))
            throw new InvalidOperationException();

        _currentState?.Leave();
        _currentState = instance;

        CurrentState = state;
        onStateChanged?.Invoke(state);

        _currentState?.Enter();
    }

    public void FixedUpdate(float dt) => _currentState?.FixedUpdate(dt);
}
