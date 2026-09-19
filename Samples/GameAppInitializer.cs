using Ssit.CrossX2;
using Ssit.CrossX2.Core;
using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.Input;
using Ssit.CrossX2.Input.Internal;
using Ssit.CrossX2.IoC;

namespace Samples;

public class GameAppComponent(IRenderer renderer) : IAppComponent
{
    public void Dispose()
    {
        // TODO release managed resources here
    }

    public void Initialize()
    {
    }

    public void SetActive(bool active)
    {
    }

    public void Update(float dt)
    {
    }

    public void Draw()
    {
        renderer.Clear(RgbaColor.CornflowerBlue);
    }

    public void Resize()
    {
        
    }
}

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
        
        return container.IoCConstruct<GameAppComponent>();
    }
    
    void IAppInitializer.RegisterServices(IIoCContainerBuilder builder)
    {
        builder
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