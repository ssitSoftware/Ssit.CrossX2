using Ssit.CrossX2;
using Ssit.CrossX2.Core;
using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.Input;
using Ssit.CrossX2.Input.Internal;
using Ssit.CrossX2.IO;
using Ssit.CrossX2.IoC;

namespace Samples;

public class GameAppInitializer: IAppInitializer
{
    IAppComponent IAppInitializer.CreateAppComponent(IIoCContainer container)
    {
        // var builder = CrossX2Ui.CreateAppBuilder(container);
        //
        // builder
        //     .WithAutoNavigationMapping(typeof(MainPage).Assembly)
        //     .WithFirstNavigation<MainPageViewModel>();
        //
        // return builder.Build();
        
        return container.IoCConstruct<GameAppTestComponent>();
    }
    
    void IAppInitializer.RegisterServices(IIoCContainerBuilder builder)
    {
        var assetsProvider = new EmbeddedFilesProvider(typeof(GameAppInitializer).Assembly, "Samples.Assets");
        
        builder
            .WithInstance<IFilesProvider>(assetsProvider)
            .WithPostBuildDelegate<IPointingDevices>(pd => pd.Mode = PointingDevicesMode.Touch | PointingDevicesMode.Mouse)
            .WithPostBuildDelegate<IInputMappings>(GameInitializer.MapInput)
            .WithSingleton<GameAppStateManager, GameAppStateManager>();
    }

    void IAppInitializer.InitializeRenderHost(IRenderHostParameters parameters)
    {
        parameters.Flags = RenderHostFlags.EnableGlowPass | RenderHostFlags.ExactSize | RenderHostFlags.EnableCrtSimulation;
        parameters.DesignSize = new Size(640, 360);
        parameters.MinScale = 2;
        parameters.MaxScale = 2;
    }
}