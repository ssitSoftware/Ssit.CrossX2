namespace Ssit.CrossX2.Framework.Content;

public interface IContentCache: IDisposable
{
    TResource Get<TResource>(string path) where TResource : class, IDisposable;
}