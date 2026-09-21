namespace Ssit.CrossX2.Framework.Input.Internal;

public interface ITouchEntity: ITouchEvent
{
    double InitialTime { get; }
    double Time { get; }
}