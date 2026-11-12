# CrossX2 Documentation

CrossX2 is a small application and game framework built around an SDL GPU backend, an IoC container, typed content loading, retained application services, and explicit rendering/audio APIs.

This is the first version of the project documentation. It focuses on the framework-level concepts needed to start and structure an application.

## Guides

- [App lifecycle](Guides/AppLifecycle.md)
- [Content loading](Guides/ContentLoading.md)
- [Rendering](Guides/Rendering.md)
- [Audio](Guides/Audio.md)

## Main Concepts

- Applications start through `AppRunner` with an `IAppInitializer`.
- Services are registered in `IAppInitializer.RegisterServices`.
- The root application object implements `IAppComponent`.
- Content is loaded through `IContentManager` and released by disposing `ResourceHandle<T>`.
- Rendering is driven by `IRenderer`, specialized renderers, and the render host.
- Audio is exposed through `ISoundManager`, `ISoundEffect`, `IMusicPlayer`, and `ICommonSoundContainer`.

## Typical Application Shape

```csharp
using Ssit.CrossX2.Framework.Core;

AppRunner.Run<MyAppInitializer>();
```

`MyAppInitializer` configures services, render host parameters, and returns the root component:

```csharp
using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.IO;
using Ssit.CrossX2.Framework.IoC;

public sealed class MyAppInitializer : IAppInitializer
{
    public string WindowTitle => "My CrossX2 App";

    public void RegisterServices(IIoCContainerBuilder builder)
    {
        builder.WithInstance<IFilesProvider>(
            new AggregatedFilesProvider()
                .AddProvider("assets:", new EmbeddedFilesProvider(typeof(MyAppInitializer).Assembly)));
    }

    public IAppComponent Initialize(IIoCContainer container, IRenderHostParameters parameters)
    {
        parameters.DesignSize = new Size(512, 288);
        parameters.MinScale = 1;
        parameters.MaxScale = 8;
        parameters.Flags = RenderHostFlags.ExactSize;

        return container.IoCConstruct<MyAppComponent>();
    }
}
```

## Document Status

These pages document the public framework surface and the current SDL backend behavior at a high level. They are intentionally concise and should grow with examples, platform notes, and API reference pages over time.
