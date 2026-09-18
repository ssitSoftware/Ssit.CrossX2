#if ANDROID

using Android.Views;
using Org.Libsdl.App;
using Ssit.CrossX2._Sdl3Impl.Input;
using Ssit.CrossX2.Core;
using Ssit.CrossX2.Input;
using Ssit.CrossX2.IoC;
using Ssit.CrossX2.Services;
using Ssit.CrossX2.Services.Internal;

namespace Ssit.CrossX2._Sdl3Impl;

public abstract class CrossXSdlActivity : SDLActivity
{
    private EventSource _eventSource;
    private NativeTextInputServiceDroid _textInputService;

    protected abstract IAppInitializer GetAppInitializer();

    protected override string[] GetLibraries() => ["SDL3", "SDL3_image", "SDL3_mixer"];

    protected override void Main()
    {
        var appInitializer = GetAppInitializer();
        AppRunnerSdl.Run(appInitializer, builder =>
        {
            builder.WithInstance<Activity>(this);
            builder.WithSingleton<INativeTextInputService, NativeTextInputServiceDroid>();
            OnConfigureServices(builder);
        }, container =>
        {
            _eventSource = (EventSource)container.Get<IEventSource>();
            _textInputService = container.Get<INativeTextInputService>() as NativeTextInputServiceDroid;
        });
    }

    protected virtual void OnConfigureServices(IIoCContainerBuilder builder) { }

    public override bool DispatchKeyEvent(KeyEvent e)
    {
        if (e != null && _textInputService?.HandleKeyEvent(e) == true)
            return true;
        return base.DispatchKeyEvent(e);
    }

    protected override void OnPause()
    {
        base.OnPause();
        _eventSource?.OnPause();
    }

    protected override void OnResume()
    {
        base.OnResume();
        _eventSource?.OnResume();
    }
}

#endif
