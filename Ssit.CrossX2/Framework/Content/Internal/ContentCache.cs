using Ssit.CrossX2.Framework.IO;

namespace Ssit.CrossX2.Framework.Content.Internal;

internal class ContentCache(IContentManager contentManager): IContentCache
{
    private readonly Dictionary<string, IDisposable> _cache = new();

    public TResource Get<TResource>(string path) where TResource: class, IDisposable
    {
        path = PathHelper.NormalizePath(path);
        var key = ContentManager.GetKey<TResource>(path);

        if (_cache.TryGetValue(key, out var content))
        {
            if (content is ResourceHandle<TResource> handle)
            {
                return handle.Resource;
            }
            throw new InvalidOperationException($"Content cache does not contain a valid resource for key '{key}'.");
        }
        
        var resourceHandle = contentManager.Get<TResource>(path);
        _cache[key] = resourceHandle;
        return resourceHandle.Resource;
    }
    
    public void Dispose()
    {
        foreach (var res in _cache)
        {
            res.Value.Dispose();
        }
    }
}