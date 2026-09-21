using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDL;
using Ssit.CrossX2.Framework.Backends.Sdl3Gpu;
using Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Input;
using Ssit.CrossX2.Framework.Input;

#if IOS

namespace Ssit.CrossX2.Framework.Core
{
    public static class AppRunner
    {
        private static Action _runAction;
    
        public static unsafe void Run(IAppInitializer appInitializer, InitializeServicesDelegate initializeServices = null)
        {
            var frameworkPath = Path.Combine(AppContext.BaseDirectory!, "Frameworks");
        
            NativeLibrary.SetDllImportResolver(typeof(SDL3).Assembly, (_, asm, path) => NativeLibrary.Load(Path.Combine(frameworkPath, "SDL3.framework/SDL3"), asm, path));
            NativeLibrary.SetDllImportResolver(typeof(SDL3_mixer).Assembly, (_, asm, path) => NativeLibrary.Load(Path.Combine(frameworkPath, "SDL3_mixer.framework/SDL3_mixer"), asm, path));
            NativeLibrary.SetDllImportResolver(typeof(SDL3_image).Assembly, (_, asm, path) => NativeLibrary.Load(Path.Combine(frameworkPath, "SDL3_image.framework/SDL3_image"), asm, path));
        
            _runAction = () =>
            {
                AppRunnerSdl.Run(appInitializer, builder =>
                {
                    builder.WithSingleton<INativeTextInputService, NativeTextInputServiceIos>();
                    initializeServices?.Invoke(builder);
                });
            };
        
            SDL3.SDL_AddEventWatch(&AppEventWatcher.AppEventWatch, IntPtr.Zero);
            SDL3.SDL_RunApp(0, null, &Run, IntPtr.Zero);
        }
    
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static unsafe int Run(int argc, byte** argv)
        {
            var runAction = _runAction;
            _runAction = null;

            runAction?.Invoke();
            return 0;
        }
    }
}

#endif