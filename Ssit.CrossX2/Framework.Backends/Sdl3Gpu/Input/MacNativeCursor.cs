using System.Runtime.InteropServices;

#if !IOS && !ANDROID

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Input;

/// <summary>
/// Hides the macOS cursor with [NSCursor hide]/[NSCursor unhide].
/// SDL_HideCursor on Cocoa only swaps the view's cursor rect for an invisible cursor, which AppKit
/// overrides on mouse movement (the cursor reappears as soon as the mouse moves). NSCursor hide is
/// reliable but global, so the caller must unhide it whenever the cursor leaves the window or the
/// window loses focus. Calls are balanced here - hide/unhide are reference counted by AppKit.
/// Must be called on the main thread (the SDL event loop thread).
/// </summary>
internal static class MacNativeCursor
{
    private const string ObjCLibrary = "/usr/lib/libobjc.A.dylib";

    [DllImport(ObjCLibrary)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjCLibrary)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjCLibrary)]
    private static extern void objc_msgSend(IntPtr receiver, IntPtr selector);

    private static bool _hidden;

    public static void SetHidden(bool hidden)
    {
        if (!OperatingSystem.IsMacOS() || hidden == _hidden)
            return;

        var nsCursor = objc_getClass("NSCursor");
        objc_msgSend(nsCursor, sel_registerName(hidden ? "hide" : "unhide"));
        _hidden = hidden;
    }
}

#endif
