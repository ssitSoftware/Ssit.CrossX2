#if IOS

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDL;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu;

internal static class AppEventWatcher
{
    public static event Action AppActivated;
    public static event Action AppDeativated;
    
    public static void CallAppActivated() => AppActivated?.Invoke();
    public static void CallAppDeativated() => AppDeativated?.Invoke();
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static unsafe SDLBool AppEventWatch(IntPtr _, SDL_Event* eventPtr)
    {
        if (eventPtr->type == (uint)SDL_EventType.SDL_EVENT_DID_ENTER_BACKGROUND)
        {
            CallAppDeativated();
        }
        
        if (eventPtr->type == (uint)SDL_EventType.SDL_EVENT_DID_ENTER_FOREGROUND)
        {
            CallAppActivated();
        }
        return false;
    }
}

#endif