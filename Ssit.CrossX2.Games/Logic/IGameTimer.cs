namespace Ssit.CrossX2.Framework.Games.Logic;

public interface IGameTimer
{
    void Update(float dt);
    float TimeDelta { get; }
}