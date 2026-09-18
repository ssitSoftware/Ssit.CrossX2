using System.ComponentModel;

namespace Ssit.CrossX2.Utils;

public static class Ids
{
    private static readonly Lock Lock = new();
    
    private static readonly Dictionary<string, ComponentId> IdsMap = new();
    private static ComponentId _nextId = new();

    public static ComponentId Get(string name)
    {
        lock (Lock)
        {
            if (IdsMap.TryGetValue(name, out var id))
            {
                return id;
            }

            id = _nextId++;
            IdsMap[name] = id;
            return id;
        }
    }
    
    public static string Get(ComponentId id)
    {
        lock (Lock)
        {
            return IdsMap.FirstOrDefault(x => x.Value == id).Key;
        }
    }
}