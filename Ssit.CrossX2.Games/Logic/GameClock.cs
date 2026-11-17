namespace Ssit.CrossX2.Framework.Games.Logic;

internal class GameClock : IGameClock
{
    public double Time { get; private set; }
    public void Update( float dt ) => Time += dt;
    public void Set(double time) => Time = time;
}