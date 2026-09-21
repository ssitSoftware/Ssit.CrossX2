using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.IoC;

namespace Ssit.CrossX2.Framework.UI.Values;

public interface IImageSource<TResource> : IDisposable where TResource: class, IDisposable
{
    event Action ImageChanged;
    ResourceHandle<TResource> GetImage(IIoCContainer container);
    Rectangle? SourceRect { get; }
}