namespace Ssit.CrossX2.Framework.Content;

/// <summary>
/// Stores loaded disposable resources and returns cached instances by path.
/// </summary>
public interface IContentCache: IDisposable
{
    TResource Get<TResource>(string path) where TResource : class, IDisposable;
}
