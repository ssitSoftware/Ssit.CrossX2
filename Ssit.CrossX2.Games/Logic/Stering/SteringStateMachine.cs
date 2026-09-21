namespace Ssit.CrossX2.Framework.Games.Logic.Stering;

public sealed class SteeringStateMachine<TObject>(TObject obj)
{
    public TObject Object { get; } = obj;
    public SteeringState<TObject> CurrentState { get; private set; }
    
    public event EventHandler<SteeringState<TObject>> OnStateChanged;
    
    public void SetState(SteeringState<TObject> state)
    {
        CurrentState?.Exit(Object);
        CurrentState = state;
        CurrentState.Enter(Object);
        
        OnStateChanged?.Invoke(this, CurrentState);
    }
}