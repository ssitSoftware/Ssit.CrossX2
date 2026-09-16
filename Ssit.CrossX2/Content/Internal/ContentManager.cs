using CrossX2.Graphics;
using CrossX2.Graphics.Misc;
using CrossX2.IO;
using CrossX2.IoC;
using CrossX2.Services;

namespace CrossX2.Content.Internal;

internal class ContentManager: IContentManager
{
    private readonly IIoCContainer _iocContainer;
    private readonly IFilesProvider _filesProvider;
    private readonly IActionScheduler _scheduler;

    public IFilesProvider FilesProvider => _filesProvider;
    
    private class ResourceInstance(IDisposable obj)
    {
        public IDisposable Object { get; } = obj;
        public readonly HashSet<Guid> Users = new();
    }
    
    private readonly Dictionary<string, ResourceInstance> _resources = new();
    private readonly Dictionary<Type, LoadResourceDelegate> _resourceLoaders = new();
    
    public ContentManager(IIoCContainer iocContainer, IFilesProvider filesProvider, IActionScheduler scheduler)
    {
        _iocContainer = iocContainer;
        _filesProvider = filesProvider;
        _scheduler = scheduler;

        RegisterLoader<ITexture>(LoadTextureFunc);
    }

    public void RemoveCache<TResource>(string path) where TResource : class, IDisposable
    {
        var key = GetKey<TResource>(path);
        if (_resources.TryGetValue(key, out var resource))
        {
            resource.Users.Remove(Guid.Empty);
            
            if (resource.Users.Count == 0)
            {
                _resources.Remove(key);
                _scheduler.Schedule(resource.Object.Dispose);
            }
        }
    }
    
    public ResourceHandle<TResource> Get<TResource>(string path) where TResource : class, IDisposable
    {
        bool cache = false;
        if (path.EndsWith('!'))
        {
            cache = true;
        }
        path = PathHelper.NormalizePath(path);
        
        var key = GetKey<TResource>(path);
        
        if (!_resources.TryGetValue(key, out var resource))
        {
            var obj = LoadResource<TResource>(path);
            
            resource = new ResourceInstance(obj);
            _resources.Add(key, resource);

            if (cache)
            {
                resource.Users.Add(Guid.Empty);
            }
            
            if (obj is IInstanceCountingResource icr)
            {
                icr.AddUser = g => resource.Users.Add(g);
                icr.RemoveUser = g =>
                {
                    resource.Users.Remove(g);
                    
                    if (resource.Users.Count == 0)
                    {
                        _resources.Remove(key);
                        _scheduler.Schedule(resource.Object.Dispose);
                    }
                };
            }
        }

        return FromResourceInstance<TResource>(key, resource);
    }

    private ResourceHandleManaged<TResource> FromResourceInstance<TResource>(string key, ResourceInstance resource)
        where TResource : class, IDisposable
    {
        var handle = new ResourceHandleManaged<TResource>((TResource)resource.Object, key, g =>
        {
            resource.Users.Remove(g);
            if (resource.Users.Count == 0)
            {
                resource.Object.Dispose();
                _resources.Remove(key);
            }
        }, () => FromResourceInstance<TResource>(key, resource));

        resource.Users.Add(handle.Guid);
        return handle;
    }

    public void RegisterLoader<TResource>(LoadResourceDelegate loadFunc) where TResource : class, IDisposable 
        => _resourceLoaders.Add(typeof(TResource), loadFunc);

    private string GetKey<TResource>(string path) where TResource : class, IDisposable 
        => path + $" ({typeof(TResource).Name})";

    private TResource LoadResource<TResource>(string path) where TResource : class, IDisposable
    {
        var type = typeof(TResource);
        if (_resourceLoaders.TryGetValue(type, out var resourceLoader))
        {
            var resource = resourceLoader.Invoke(path);
            if (resource is TResource resourceInstance) return resourceInstance;
            
            resource.Dispose();
            throw new Exception($"Resource loading failed for type {type.Name} on path {path}");
        }

        using var stream = _filesProvider.Open(path);
        return _iocContainer.IoCConstruct<TResource>(stream);
    }
    
    private IDisposable LoadTextureFunc(string path)
    {
        var name = Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path));
        var ext = Path.GetExtension(path).TrimEnd('!');

        var glowPath =  name + ".glow" + ext;
        
        var hasDiffuseImplicit = _filesProvider.FileExists(name + ext);
        var hasDiffuseExplicit = _filesProvider.FileExists(name + ".diffuse" + ext);
        var hasNormals = _filesProvider.FileExists(name + ".normal" + ext);
        var hasGlow = _filesProvider.FileExists(name + ".glow" + ext);
        
        if (!hasGlow)
        {
            glowPath = hasDiffuseImplicit ? name + ext : hasDiffuseExplicit ? name + ".diffuse" + ext : null;
            hasGlow = glowPath != null;
        }
        
        return _iocContainer.IoCConstruct<ITexture>(new LoadTextureParameters
        {
            DiffuseMapStream =  hasDiffuseImplicit ? _filesProvider.Open(name + ext) : hasDiffuseExplicit ? 
                _filesProvider.Open(name + ".diffuse" + ext) : null,
            
            NormalMapStream = hasNormals ? _filesProvider.Open(name + ".normal" + ext) : null,
            GlowMapStream = hasGlow ? _filesProvider.Open(glowPath) : null
        });
    }
}