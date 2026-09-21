#if !IOS  && !ANDROID

using Ssit.CrossX2.Framework.Backends.Sdl3Gpu;

namespace Ssit.CrossX2.Framework.Core;

public static class AppRunner
{
    public static void Run(IAppInitializer appInitializer, InitializeServicesDelegate registerServices = null, InitializeAppDelegate configure = null) 
        => AppRunnerSdl.Run(appInitializer, registerServices, configure);
    
    public static void Run<TAppInitializer>(InitializeServicesDelegate registerServices = null, InitializeAppDelegate configure = null) 
        where TAppInitializer: IAppInitializer, new() 
        => AppRunnerSdl.Run(new TAppInitializer(), registerServices, configure);
}

#endif