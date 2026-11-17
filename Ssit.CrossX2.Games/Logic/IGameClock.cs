namespace Ssit.CrossX2.Framework.Games.Logic;

public interface IGameClock
{
    double Time { get; }
    void Update(float dt);
    void Set(double time);
}