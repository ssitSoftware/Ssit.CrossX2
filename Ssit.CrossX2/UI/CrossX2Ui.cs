using Ssit.CrossX2.IoC;

namespace Ssit.CrossX2.UI;

public static class CrossX2Ui
{
    public static IUiAppBuilder CreateAppBuilder(IIoCContainer container) => container.IoCConstruct<UiAppBuilder>();
}