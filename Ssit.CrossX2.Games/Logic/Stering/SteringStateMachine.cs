namespace Ssit.CrossX2.Framework.Games.Logic.Stering;

public sealed class SteeringStateMachine<TObject>(TObject obj)
{
    public bool Debug { get; set; }
    
    public TObject Object { get; } = obj;
    public SteeringState<TObject> CurrentState { get; private set; }
    
    public event EventHandler<SteeringState<TObject>> OnStateChanged;
    
    /// <summary>
    /// Exits the current state without entering a new one.
    /// </summary>
    public void Reset()
    {
        CurrentState?.Exit(Object);
        CurrentState = null;
    }

    public void SetState(SteeringState<TObject> state)
    {
        CurrentState?.Exit(Object);
        CurrentState = state;
        CurrentState.Enter(Object);
        
        OnStateChanged?.Invoke(this, CurrentState);

        if (Debug)
        {
            Console.WriteLine($"SteeringStateMachine<{typeof(TObject).Name}>: {CurrentState.Name}");
        }
    }
}