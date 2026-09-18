#if !IOS  && !ANDROID

using Ssit.CrossX2._Sdl3Impl;

namespace Ssit.CrossX2.Core;

public static class AppRunner
{
    public static void Run(IAppInitializer appInitializer, InitializeServicesDelegate registerServices = null, InitializeAppDelegate configure = null) 
        => AppRunnerSdl.Run(appInitializer, registerServices);
    
    public static void Run<TAppInitializer>(InitializeServicesDelegate registerServices = null, InitializeAppDelegate configure = null) 
        where TAppInitializer: IAppInitializer, new() 
        => AppRunnerSdl.Run(new TAppInitializer(), registerServices, configure);
}

#endif