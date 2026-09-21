using System.Collections.Concurrent;
using Ssit.CrossX2.Framework.Services;

namespace Ssit.CrossX2.Framework.Games.Logic;

public class GameState(IActionScheduler actionScheduler) : IGameState
{
    public event Action StateUpdated;
    
    private readonly ConcurrentDictionary<string, bool> _flags = new();
    
    public bool HasFlag(string flag) => _flags.TryGetValue(flag, out _);

    public void SetFlags(string flags)
    {
        foreach (var flag in flags.Split('|'))
        {
            _flags.TryAdd(flag, true);
        }

        actionScheduler.Schedule(() => StateUpdated?.Invoke());
    }
}