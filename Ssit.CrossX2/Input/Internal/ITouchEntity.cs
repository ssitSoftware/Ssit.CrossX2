namespace Ssit.CrossX2.Input.Internal;

public interface ITouchEntity: ITouchEvent
{
    double InitialTime { get; }
    double Time { get; }
}