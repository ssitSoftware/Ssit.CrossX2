using Ssit.CrossX2.Framework.Games.Logic.Checkpoints;

namespace Ssit.CrossX2.Framework.Games.Logic;

/// <param name="gameInfo">
/// Total game time of the slot being played, shared by the game instances of the slot (null if not registered).
/// </param>
/// <param name="slot">Save slot of the game instance.</param>
internal class GameClock(GameInfoService gameInfo = null, int slot = 0) : IGameClock
{
    /// <summary>
    /// Total game time of this game instance - used when the slot isn't tracked by <paramref name="gameInfo"/>.
    /// </summary>
    private double _instanceTotalGameTime;

    public double Time { get; private set; }

    public double TotalGameTime => gameInfo?.Slot == slot ? gameInfo.TotalGameTime : _instanceTotalGameTime;

    public void Update(float dt)
    {
        Time += dt;
        _instanceTotalGameTime += dt;
        gameInfo?.Add(slot, dt);
    }

    public void Set(double time) => Time = time;
}