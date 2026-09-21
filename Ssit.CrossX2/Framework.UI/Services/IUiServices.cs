using Ssit.CrossX2.Framework.IoC;

namespace Ssit.CrossX2.Framework.UI.Services;

public interface IUiServices
{
    public IIoCContainer IoCContainer { get; }
    public IHandlerMapper HandlerMapper { get; }
}