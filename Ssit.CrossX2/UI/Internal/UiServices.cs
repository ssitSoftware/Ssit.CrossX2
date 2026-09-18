using Ssit.CrossX2.IoC;
using Ssit.CrossX2.UI.Services;

namespace Ssit.CrossX2.UI.Internal;

public class UiServices: IUiServices
{
    public UiServices(IIoCContainer ioCContainer, IHandlerMapper handlerMapper)
    {
        IoCContainer = ioCContainer;
        HandlerMapper = handlerMapper;
    }

    public IIoCContainer IoCContainer { get; }
    public IHandlerMapper HandlerMapper { get; }
}