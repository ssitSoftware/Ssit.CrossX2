using Samples.UI.Pages;
using Samples.UI.ViewModels;
using Ssit.CrossX2;
using Ssit.CrossX2.Core;
using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.Input;
using Ssit.CrossX2.Input.Internal;
using Ssit.CrossX2.IoC;
using Ssit.CrossX2.UI;

namespace Samples;

public class GameAppInitializer: IAppInitializer
{
    IAppComponent IAppInitializer.CreateAppComponent(IIoCContainer container)
    {
        var builder = CrossX2Ui.CreateAppBuilder(container);

        builder
            .WithAutoNavigationMapping(typeof(MainPage).Assembly)
            .WithFirstNavigation<MainPageViewModel>();

        return container.IoCConstruct<GameAppComponent>(builder.Build());
    }
    
    void IAppInitializer.RegisterServices(IIoCContainerBuilder builder)
    {
        builder
            .WithPostBuildDelegate<IPointingDevices>(pd => pd.Mode = PointingDevicesMode.Touch | PointingDevicesMode.Mouse)
            .WithPostBuildDelegate<IInputMappings>(GameInitializer.MapInput);
    }

    void IAppInitializer.InitializeRenderHost(IRenderHostParameters parameters)
    {
        parameters.Flags = RenderHostFlags.EnableGlowPass | RenderHostFlags.ExactSize | RenderHostFlags.EnableCrtSimulation;
        parameters.DesignSize = new Size(640, 360);
        parameters.MinScale = 2;
        parameters.MaxScale = 2;
    }
}