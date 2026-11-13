namespace Ssit.CrossX2.Framework.Games.Platformer.Builders;

internal class LinkMap: ILinkMap, IMapObjects
{
    private readonly Dictionary<int, MapObjectEntry> _links = new();
    private readonly List<MapObjectEntry> _entries = new();

    private readonly List<Action> _requests = new();

    public IReadOnlyList<MapObjectEntry> All => _entries;
    
    public void AddMapping(int id, string typeId, object obj)
    {
        var entry = new MapObjectEntry(id, typeId, obj);
        _links.Add(id, entry);
        _entries.Add(entry);
    }

    public bool TryGet(int mapId, out MapObjectEntry entry) => _links.TryGetValue(mapId, out entry);

    public void RequestLink<TLink>(int id, Action<TLink> action) where TLink : class
    {
        _requests.Add(() =>
        {
            var link = GetLink<TLink>(id);
            action(link);
        });
    }
    
    private TLink GetLink<TLink>(int id) where TLink : class
    {
        if (!_links.TryGetValue(id, out var entry))
        {
            return null;
        }
        return entry.Instance as TLink;
    }

    public void ResolveLinks()
    {
        foreach (var request in _requests)
        {
            request();
        }
        _requests.Clear();
    }
}
