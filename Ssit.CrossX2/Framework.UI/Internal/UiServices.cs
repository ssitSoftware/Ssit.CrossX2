using Ssit.CrossX2.Framework.IoC;
using Ssit.CrossX2.Framework.UI.Services;

namespace Ssit.CrossX2.Framework.UI.Internal;

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