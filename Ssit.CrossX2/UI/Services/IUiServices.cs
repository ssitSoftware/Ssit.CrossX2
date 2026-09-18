using Ssit.CrossX2.IoC;

namespace Ssit.CrossX2.UI.Services;

public interface IUiServices
{
    public IIoCContainer IoCContainer { get; }
    public IHandlerMapper HandlerMapper { get; }
}