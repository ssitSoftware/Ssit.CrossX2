namespace CrossX2;

public static class Ids
{
    private static readonly Lock Lock = new();
    
    private static readonly Dictionary<string, uint> IdsMap = new();
    private static uint _nextId = 1;

    public static uint Get(string name)
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
    
    public static string Get(uint id)
    {
        lock (Lock)
        {
            return IdsMap.FirstOrDefault(x => x.Value == id).Key;
        }
    }
}