namespace Ssit.CrossX2.Framework.Content;

public class ReferenceCountedResource<TResource>: IDisposable where TResource : class, IDisposable
{
    private class ResourceWithCounter(TResource resource)
    {
        public TResource Resource { get; private set; } = resource;

        private int _count;
        
        public void Attach()
        {
            _count++;
        }
        
        public void Detach()
        {
            _count--;

            if (_count > 0) return;

            Resource?.Dispose();
            Resource = null;
        }
    }
    
    public TResource Resource => _resourceWithCounter.Resource;
    private readonly ResourceWithCounter _resourceWithCounter;
    
    public ReferenceCountedResource(TResource resource)
    {
        _resourceWithCounter = new ResourceWithCounter(resource);
        _resourceWithCounter.Attach();
    }
    
    ~ReferenceCountedResource()
    {
        _resourceWithCounter.Detach();
    }

    private ReferenceCountedResource(ResourceWithCounter resourceWithResourceWithCounter)
    {
        _resourceWithCounter = resourceWithResourceWithCounter;
        _resourceWithCounter.Attach();
    }

    public ReferenceCountedResource<TResource> Clone() => new(_resourceWithCounter);

    public void Dispose()
    {
        _resourceWithCounter.Detach();
        GC.SuppressFinalize(this);
    }
}