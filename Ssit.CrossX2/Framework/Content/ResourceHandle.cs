namespace Ssit.CrossX2.Framework.Content;

public abstract class ResourceHandle<TResource> : IDisposable where TResource : class, IDisposable
{
    /// <summary>
    /// Resource reference.
    /// </summary>
    public TResource Resource { get; }
    
    /// <summary>
    /// Resource name.
    /// </summary>
    public string Name { get; }
    
    internal ResourceHandle(TResource resource, string name)
    {
        Resource = resource;
        Name = name;
    }

    public void Dispose()
    {
        OnDispose();
    }

    protected abstract void OnDispose();
    
    public static implicit operator TResource( ResourceHandle<TResource> handle ) => handle.Resource;

    public abstract ResourceHandle<TResource> Clone();
}

/// <summary>
/// Represents disposable handle to the resource. 
/// </summary>
/// <typeparam name="TResource">Type of the underlying resource.</typeparam>
public class ResourceHandleManaged<TResource> : ResourceHandle<TResource> where TResource : class, IDisposable
{
    internal Guid Guid { get; } = Guid.NewGuid();

    private readonly Action<Guid> _onDispose;
    private readonly Func<ResourceHandle<TResource>> _cloneDelegate;

    internal ResourceHandleManaged(TResource resource, string name, Action<Guid> onDispose, Func<ResourceHandle<TResource>> cloneDelegate):
        base(resource, name)
    {
        _onDispose = onDispose;
        _cloneDelegate = cloneDelegate;
    }
    
    /// <summary>
    /// Disposes resource handle.
    /// </summary>
    protected override void OnDispose()
    {
        _onDispose?.Invoke(Guid);
    }

    public override ResourceHandle<TResource> Clone() => _cloneDelegate();
}

public class ResourceHandleUnmanaged<TResource>(TResource resource, string name)
    : ResourceHandle<TResource>(resource, name)
    where TResource : class, IDisposable
{
    private bool _isDisposed;

    protected override void OnDispose()
    {
        if (_isDisposed)
            return;
        
        Resource?.Dispose();
        _isDisposed = true;
    }

    public override ResourceHandle<TResource> Clone()
    {
        return this;
    }
}