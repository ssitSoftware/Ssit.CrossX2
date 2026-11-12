# App Lifecycle

CrossX2 applications are started through `AppRunner`. The runner creates backend services, asks the application to register its own services, builds the IoC container, creates the root component, and enters the update/render loop.

## Startup Flow

1. `AppRunner.Run(...)` starts the platform backend.
2. Built-in services are registered, including input, audio, content, rendering, timers, and window services.
3. Optional startup delegates can add or configure services.
4. `IAppInitializer.RegisterServices` registers application services.
5. The IoC container is built.
6. `IAppInitializer.Initialize` receives the built container and `IRenderHostParameters`.
7. The returned `IAppComponent` is initialized and activated.
8. The main loop processes input, updates services/components, renders, and dispatches lifecycle events.

## IAppInitializer

`IAppInitializer` is the application entry contract.

```csharp
public interface IAppInitializer
{
    string WindowTitle { get; }
    void RegisterServices(IIoCContainerBuilder builder);
    IAppComponent Initialize(IIoCContainer container, IRenderHostParameters parameters);
    bool ShouldInitializePortraitApp => false;
}
```

Use `RegisterServices` for dependencies and `Initialize` for render-host settings and root component creation.

## IAppComponent

The root component receives the main lifecycle callbacks.

```csharp
public interface IAppComponent : IDisposable
{
    void Initialize();
    void SetActive(bool active);
    void Update(float dt);
    void Draw();
    void Resize();
}
```

`SetActive(false)` is called when the app is backgrounded or inactive. `Resize` is called when render target sizing changes. `Dispose` is called before services are disposed.

## Update Order

During a normal frame, the SDL runner:

1. Processes scheduled main-thread actions.
2. Updates pointing devices and keyboard state.
3. Raises `IEventSource.Updating`.
4. Calls `IUpdatable.Update` on registered services.
5. Calls `IAppComponent.Update`.
6. Raises `IEventSource.Updated`.
7. Renders the component.
8. Raises `IEventSource.RenderFinished`.

Use `IUpdatable` for services that need framework-driven update callbacks.

## Lifecycle Events

`IEventSource` exposes framework events:

- `Updating(float dt)`
- `Updated`
- `RenderFinished`
- `Paused`
- `Resumed`
- `EnteredFullscreen`
- `LeftFullscreen`

These are useful for subsystems that should react without being the root component.

## Window and Platform State

`IAppWindowManager` controls closing, fullscreen/windowed state, title, size, and display mode. On Android, applications derive from `CrossXSdlActivity`; on desktop and iOS, use `AppRunner`.
