namespace Ssit.CrossX2.Framework.Games.Logic;

public interface IGameState
{
    event Action StateUpdated;
    bool HasFlag(string flag);
    void SetFlags(string flag);
}