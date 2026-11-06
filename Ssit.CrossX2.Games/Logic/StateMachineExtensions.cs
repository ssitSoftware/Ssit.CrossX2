namespace Ssit.CrossX2.Framework.Games.Logic;

public static class StateMachineExtensions
{
    public static StateMachine<TObject> WithBehaviorState<TObject>(this StateMachine<TObject> sm, string name, params Behavior<TObject>[] behaviors)
    {
        var state = new State<TObject>(behaviors);
        sm.AddState(name, state);
        return sm;
    }
}