using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Graphics.Misc;
using Ssit.CrossX2.Framework.IoC;
using Ssit.CrossX2.Framework.UI.Services;

namespace Ssit.CrossX2.Framework.UI.Values;

public sealed class ImageSource<TTexture> : IImageSource<TTexture> where TTexture : class, IDisposable
{
    public static implicit operator ImageSource<TTexture>(string path) => new(path);
    public static implicit operator ImageSource<TTexture>((string path, Rectangle rect) o) => new(o.path, o.rect);
    public static implicit operator ImageSource<TTexture>(Uri uri) => new(uri);
    
    private string _resourcePath;
    private Uri _resourceUri;
    
    private Task<ResourceHandle<TTexture>> _loadTextureTask;
    private CancellationTokenSource _cancellationTokenSource;
    
    private readonly object _lock = new();
    
    private ResourceHandle<TTexture> _texture;

    public event Action ImageChanged;

    private bool _reload;

    public bool IsLoading
    {
        get
        {
            lock (_lock)
            {
                return _loadTextureTask is not null;
            }
        }
    }
    
    public ResourceHandle<TTexture> Texture
    {
        set
        {
            lock (_lock)
            {
                if (_loadTextureTask != null)
                {
                    throw new InvalidOperationException("Cannot change image while loading image in background.");
                }
                
                _texture?.Dispose();
                _texture = value;
                ImageChanged?.Invoke();
            }
        }
    }
    
    public ResourceHandle<TTexture> GetImage(IIoCContainer container)
    {
        lock (_lock)
        {
            if (_texture == null || _reload)
            {
                if (_loadTextureTask == null)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    _loadTextureTask = LoadTextureFromParameters(container, _cancellationTokenSource.Token);
                }

                _reload = false;
            }

            if (_loadTextureTask is { IsCompleted: true })
            {
                if (_loadTextureTask.IsCompletedSuccessfully)
                {
                    _texture?.Dispose();
                    _texture = _loadTextureTask.Result;
                }
                _loadTextureTask = null;
                _cancellationTokenSource = null;
            }

            return _texture;
        }
    }

    public Rectangle? SourceRect { get; private set; }

    public void SetSource(string path, Rectangle? sourceRect = null)
    {
        lock (_lock)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
            _loadTextureTask = null;
            
            _resourcePath = path;
            _reload = true;

            SourceRect = sourceRect;
        }
    }

    public void SetSource(Uri uri)
    {
        lock (_lock)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
            _loadTextureTask = null;
            
            _resourceUri = uri;
            _reload = true;
        }
    }
    
    public ImageSource()
    {
    }
    
    public ImageSource(string resourcePath, Rectangle? sourceRect = null)
    {
        _resourcePath = resourcePath;
        SourceRect = sourceRect;
    }

    public ImageSource(Uri uri)
    {
        _resourceUri = uri;
    }

    private async Task<ResourceHandle<TTexture>> LoadTextureFromParameters(IIoCContainer container, CancellationToken cancellationToken)
    {
        ResourceHandle<TTexture> result = null;
        if (_resourcePath is not null)
        {
            result = await LoadContent(container, cancellationToken);
        }

        if (_resourceUri is not null)
        {
            result = await LoadFromUri(container, cancellationToken);
        }

        lock (_lock)
        {
            if (result is not null)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    result.Dispose();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                container.Get<IUiActionDispatcher>().Enqueue(() => ImageChanged?.Invoke());
                return result;
            }
        }

        throw new InvalidOperationException("Cannot load image from resource path.");
    }

    private Task<ResourceHandle<TTexture>> LoadContent(IIoCContainer container, CancellationToken _)
    {
        ResourceHandle<TTexture> result = null;
        lock (_lock)
        {
            if (_resourcePath is not null)
            {
                result = container.Get<IContentManager>().Get<TTexture>(_resourcePath);
                _resourcePath = null;
            }
        }

        if (result is not null)
        {
            return Task.FromResult(result);
        }
        
        return Task.FromException<ResourceHandle<TTexture>>(new InvalidProgramException("Cannot load image from resource path."));
    }
    
    private async Task<ResourceHandle<TTexture>> LoadFromUri(IIoCContainer container,
        CancellationToken cancellationToken)
    {
        Uri uri;
        lock (_lock)
        {
            uri = _resourceUri;
            _resourceUri = null;
        }

        var memoryStream = new MemoryStream();
        using (HttpClient client = new HttpClient())
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            cancellationToken.ThrowIfCancellationRequested();
            
            var response = await client.SendAsync(request, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            
            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync();
                cancellationToken.ThrowIfCancellationRequested();
                await stream.CopyToAsync(memoryStream, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        if (memoryStream.Length == 0)
        {
            throw new InvalidOperationException("Cannot load image from resource path.");
        }
        
        memoryStream.Seek(0, SeekOrigin.Begin);
        var texture = container.IoCConstruct<TTexture>(new LoadTextureParameters
        {
            DiffuseMapStream = memoryStream
        });

        if (cancellationToken.IsCancellationRequested)
        {
            texture.Dispose();
            cancellationToken.ThrowIfCancellationRequested();
        }
        
        return new ResourceHandleUnmanaged<TTexture>(texture, uri.AbsoluteUri);
    }

    public void Dispose()
    {
        var tokenSrc = _cancellationTokenSource;
        var disposable = _texture;
        
        lock (_lock)
        {
            _texture = null;
            _cancellationTokenSource = null;
        }

        tokenSrc?.Cancel();
        disposable?.Dispose();
    }
}