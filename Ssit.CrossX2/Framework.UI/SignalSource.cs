namespace Ssit.CrossX2.Framework.UI;

public class SignalSource
{
    public event Action Signal;
    public void Trigger() => Signal?.Invoke();
}

public class SignalSource<TArgument>
{
    public event Action<TArgument> Signal;
    public void Trigger(TArgument arg) => Signal?.Invoke(arg);
}