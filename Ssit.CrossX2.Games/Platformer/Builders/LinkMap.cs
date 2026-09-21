namespace Ssit.CrossX2.Framework.Games.Platformer.Builders;

internal class LinkMap: ILinkMap
{
    private readonly Dictionary<int, object> _links = new();

    private readonly List<Action> _requests = new();
    
    public void AddMapping(int id, object obj)
    {
        _links.Add(id, obj);
    }

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
        if (!_links.TryGetValue(id, out var obj))
        {
            return null;
        }
        return obj as TLink;
    }

    public void ResolveLinks()
    {
        foreach (var request in _requests)
        {
            request();
        }
    }
}