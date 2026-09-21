using Ssit.CrossX2.Framework.IoC;

namespace Ssit.CrossX2.Framework.UI;

public static class CrossX2Ui
{
    public static IUiAppBuilder CreateAppBuilder(IIoCContainer container) => container.IoCConstruct<UiAppBuilder>();
}