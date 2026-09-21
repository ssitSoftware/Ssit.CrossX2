using Ssit.CrossX2.Framework.IO;

namespace Ssit.CrossX2.Framework.Content;

public interface IContentManager
{
    /// <summary>
    /// Gets resource from given path amd returns handle to it. Underneath loads resource as needed.
    /// </summary>
    /// <param name="path">Path to resource file.</param>
    /// <typeparam name="TResource">Type of resource to load.</typeparam>
    /// <returns>Disposable handle to the resource instance.</returns>
    /// <remarks>Each handle must be disposed, when no longer in use. When all handles are disposed for a specific resource, it is unloaded and released.</remarks>
    ResourceHandle<TResource> Get<TResource>(string path) where TResource : class, IDisposable;
    
    /// <summary>
    /// Registers custom resource loader.
    /// </summary>
    /// <param name="loadFunc">Loading method delegate.</param>
    /// <typeparam name="TResource">Type of resource, which custom loading should be registered.</typeparam>
    void RegisterLoader<TResource>(LoadResourceDelegate loadFunc) where TResource: class, IDisposable;
    
    void RemoveCache<TResource>(string path) where TResource: class, IDisposable;
    
    IFilesProvider FilesProvider { get; }
}