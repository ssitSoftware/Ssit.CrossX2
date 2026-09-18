using Ssit.CrossX2.Content;
using Ssit.CrossX2.IoC;

namespace Ssit.CrossX2.UI.Values;

public interface IImageSource<TResource> : IDisposable where TResource: class, IDisposable
{
    event Action ImageChanged;
    ResourceHandle<TResource> GetImage(IIoCContainer container);
    Rectangle? SourceRect { get; }
}