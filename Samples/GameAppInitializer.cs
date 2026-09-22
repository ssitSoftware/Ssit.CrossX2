using Ssit.CrossX2.Fonts.RetroPixel;
using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Effects;
using Ssit.CrossX2.Framework.Graphics.Font;
using Ssit.CrossX2.Framework.Input;
using Ssit.CrossX2.Framework.Input.Internal;
using Ssit.CrossX2.Framework.IO;
using Ssit.CrossX2.Framework.IoC;

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
        var assetsProvider =
            new AggregatedFilesProvider()
                .AddProvider("assets:", new EmbeddedFilesProvider(typeof(GameAppInitializer).Assembly, "Samples.Assets"))
                .AddProvider(RetroPixelFonts.Source.DriveName, RetroPixelFonts.Source.FilesProvider);
        
        builder
            .WithInstance<IFilesProvider>(assetsProvider)
            .WithInstance(new CrtSimulationEffectParameters
            {
                ScanlineIntensity = 0.75f,
                RgbDisplacement = 0.25f,
                BarrelDistortion = 0,
                RestoreLightness = 1.33f,
                Vignette = 0.5f,
                BleedFactor = 1.0f
            })
            .WithSingleton<GameAppStateManager, GameAppStateManager>()
            .WithPostBuildDelegate<IPointingDevices>(pd => pd.Mode = PointingDevicesMode.Touch | PointingDevicesMode.Mouse)
            .WithPostBuildDelegate<IInputMappings>(GameInitializer.MapInput)
            .WithPostBuildDelegate<IFontsManager>(fm => fm.LoadFonts(RetroPixelFonts.Source.DefinitionPath));
    }

    void IAppInitializer.InitializeRenderHost(IRenderHostParameters parameters)
    {
        parameters.Flags = RenderHostFlags.EnableGlowPass | RenderHostFlags.ExactSize | RenderHostFlags.EnableCrtSimulation;
        parameters.DesignSize = new Size(640, 360);
        parameters.MinScale = 1;
        parameters.MaxScale = 1;
    }
}