# Content Loading

CrossX2 loads assets through `IContentManager`. It combines virtual file providers, typed resource loaders, and disposable resource handles.

## File Providers

Assets are read through `IFilesProvider`.

```csharp
public interface IFilesProvider
{
    Stream Open(string path);
    bool FileExists(string path);
    string[] GetFiles(string path, string extension = null);
    string GetPhisicalFilePath(string path);
}
```

Common providers:

- `EmbeddedFilesProvider` reads assembly embedded resources.
- `LocalFilesProvider` reads files from the local filesystem.
- `BundleFilesProvider` reads application bundle resources.
- `CacheFilesProvider` copies another provider into application data and reads from the cache.
- `AggregatedFilesProvider` routes virtual drive prefixes to one or more providers.

Typical setup:

```csharp
builder.WithInstance<IFilesProvider>(
    new AggregatedFilesProvider()
        .AddProvider("assets:", new EmbeddedFilesProvider(typeof(MyAppInitializer).Assembly, "MyApp.Assets"))
        .AddProvider("bundle:", new BundleFilesProvider()));
```

Paths are normalized by `PathHelper`. A path such as `assets:/Sprites/Hero.png` is resolved by the provider registered for the `assets:` drive.

## Content Manager

Use `IContentManager.Get<T>` to load a typed resource.

```csharp
ResourceHandle<ITexture> texture = contentManager.Get<ITexture>("assets:/UI/Button.png");
```

The returned handle owns one reference to the resource. Dispose the handle when the owner no longer needs it:

```csharp
texture.Dispose();
```

When all handles for a resource are disposed, the resource is released. `ResourceHandle<T>.Clone()` creates another handle to the same resource.

## Built-In Loaders

The default content manager registers loaders for:

- `ITexture`
- `Sprite`
- `SpriteEx`
- `DualTexture`

Texture loading supports related map files next to the diffuse image:

- `Name.png` or `Name.diffuse.png` for diffuse color.
- `Name.glow_.png` for glow.
- `Name.light.png` for light.
- `Name.glow.png` for combined glow/light behavior.

## Custom Loaders

Register resource loaders with `RegisterLoader<TResource>`.

```csharp
contentManager.RegisterLoader<MyResource>(path =>
{
    using var stream = contentManager.FilesProvider.Open(path);
    return MyResource.Load(stream);
});
```

The loader returns an `IDisposable`. The content manager stores it and releases it when no active handle remains.

## Persistent Storage

Use `IFileStorage` for application data rather than bundled assets.

```csharp
storage.WriteText("settings.json", json);
var json = storage.ReadText("settings.json");
```

`FilesStorage` stores files under the user's local application data directory for the configured application name.
