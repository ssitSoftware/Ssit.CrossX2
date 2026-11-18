using System.Collections.Concurrent;
using Ssit.CrossX2.Framework.Services;

namespace Ssit.CrossX2.Framework.Games.Logic;

public class GameState(IActionScheduler actionScheduler) : IGameState
{
    public event Action StateUpdated;
    public event Action<IReadOnlyList<string>> FlagsSet;
    
    private readonly ConcurrentDictionary<string, bool> _flags = new();
    
    public bool HasFlag(string flag) => _flags.TryGetValue(flag, out _);

    public void SetFlags(string flags)
    {
        var newFlags = new List<string>();

        foreach (var flag in flags.Split('|'))
        {
            if (_flags.TryAdd(flag, true))
            {
                newFlags.Add(flag);
            }
        }

        actionScheduler.Schedule(() => StateUpdated?.Invoke());

        if (newFlags.Count > 0)
        {
            actionScheduler.Schedule(() => FlagsSet?.Invoke(newFlags));
        }
    }

    public IReadOnlyCollection<string> Flags => _flags.Keys.ToArray();

    public void RestoreFlags(IEnumerable<string> flags)
    {
        _flags.Clear();
        
        foreach (var flag in flags)
        {
            _flags.TryAdd(flag, true);
        }

        actionScheduler.Schedule(() => StateUpdated?.Invoke());
    }
}