using System.Numerics;
using System.Diagnostics;
using System.Reflection;
using Ssit.CrossX2.Framework.Audio;
using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.Games.Logic.Checkpoints;
using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Map;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;
using Ssit.CrossX2.Framework.Games.Rendering;
using Ssit.CrossX2.Framework.Games.Rendering.Map;
using Ssit.CrossX2.Framework.Games.Template;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.IO;
using Ssit.CrossX2.Framework.IoC;
using Ssit.CrossX2.Framework.Services;

namespace Ssit.CrossX2.Framework.Games.Logic;

public class AabbGameInstance : IGameInstance, IMessenger, IGameRendererFilter
{
    public interface IBackgroundRenderer: IDisposable
    {
        void Render(IRenderer renderer);
        void Update(float deltaTime);
    }
    
    public class Parameters
    {
        public string MapPath { get; set; }

        /// <summary>
        /// Common prefix of the map paths (e.g. "assets:/Game/Maps/"). It is cut off the map path to get the map id
        /// (<see cref="MapPathHelper.MapId"/>) naming the map files in the application storage: cache and checkpoint state.
        /// </summary>
        public string MapPathPrefix { get; set; }
        
        public Action<IIoCContainerBuilder> RegisterServices { get; set; }
        public IMaterial[] Materials { get; set; }
        public int BackgroundColorIndex { get; set; }
        public int? MinPhisicsUPS { get; set; }
        public int? MaxPhisicsUPS { get; set; }
        public IBackgroundRenderer BackgroundRenderer { get; set; }
        
        /// <summary>
        /// Checkpoint restored right after the map is loaded (map objects, game state and player state).
        /// Its <see cref="CheckpointData.MapPath"/> must match <see cref="MapPath"/>.
        /// </summary>
        public CheckpointData RestoreFrom { get; set; }
        
        /// <summary>
        /// Save slot the game is played in.
        /// </summary>
        public int Slot { get; set; }

        /// <summary>
        /// Store a JPEG preview of the screen in each checkpoint (<see cref="CheckpointData.Preview"/>, "Preview.jpg" in the slot directory).
        /// </summary>
        public bool StoreCheckpointPreview { get; set; }

        /// <summary>
        /// How many times the checkpoint preview is smaller than the rendered frame.
        /// </summary>
        public float CheckpointPreviewDownSize { get; set; } = 4;
    }
    
    /// <summary>
    /// Directory of the map caches (static colliders) in the application storage.
    /// </summary>
    public const string CacheDirectory = "Cache";

    /// <summary>
    /// Suffix of the map cache file name, added to the map id: "Map01" → "Cache/Map01Cache.bin". The cache is binary.
    /// </summary>
    public const string CacheFileSuffix = "Cache.bin";
    
    IIoCContainer IGameInstance.Services => Container;
    private readonly IContentCache _contentCache;

    public event Action<float> FixedUpdate;
    public event Action Updated;
    public event Action<object> Message;
    public IMessenger Messenger => this;
    
    private readonly IGameTimer _timer;
    private readonly IGameClock _clock;

    private readonly IActionScheduler _scheduler;
    private readonly IGameTemplate _gameTemplate;
    private readonly MapDisplayElement _mapDisplayElement;
    
    private readonly IBackgroundRenderer _backgroundRenderer;
    
    // ReSharper disable once MemberCanBePrivate.Global
    public readonly ISimulation Simulation;
    public readonly IIoCContainer Container;
    private readonly ICamera _camera;

    private bool _isStarted;
    
    public int FilterMode { get; set; } = 0;
    
    private bool _isDisposed;

    private readonly Queue<object> _messages = new();

    int IGameInstance.RenderPasses => 1;
    
    public int Slot { get; }
    
    private readonly Stopwatch _internalStopwatch = new();
    private TimeSpan _lastTimeElapsed = TimeSpan.Zero;
    private float _lastDeltaTime = 0;

    private readonly MapLightsContainer _lightsContainer;
    private bool _isActive;

    void IGameInstance.Render(IRenderer renderer, RectangleF target, int renderPass, float scale)
    {
        if (renderPass != 0)
            return;
        
        Render(renderer, target, scale);
    }

    void IGameInstance.RenderDebug(IRenderer renderer, RectangleF target, float scale) => RenderDebug(renderer, target, scale);

    public AabbGameInstance(IIoCContainer container, IContentManager contentManager,
        IActionScheduler scheduler, IGameTemplate gameTemplate, IFileStorage storage,
        Parameters parameters)
    {
        _contentCache = container.IoCConstruct<IContentCache>();
        _timer = container.IoCConstruct<GameTimer>(new GameTimer.Parameters
        {
            MinUps = parameters.MinPhisicsUPS,
            MaxUps = parameters.MaxPhisicsUPS
        });

        _internalStopwatch.Start();
        
        Slot = parameters.Slot;
        
        // The total game time of the slot is shared by its game instances (registered in the application services).
        container.TryGet<GameInfoService>(out var gameInfo);
        _clock = new GameClock(gameInfo, Slot);
        
        _scheduler = scheduler;
        _gameTemplate = gameTemplate;
        _backgroundRenderer = parameters.BackgroundRenderer;
        
        var mapId = MapPathHelper.MapId(parameters.MapPath, parameters.MapPathPrefix);
        
        using var stream = contentManager.FilesProvider.Open(parameters.MapPath);
        var map = MapFile.FromStream(stream, gameTemplate);

        var tilesets = map.Tilesets.Select(contentManager.Get<ITexture>).ToArray();

        _lightsContainer = _gameTemplate.EnableLighting ? new MapLightsContainer() : null;
        
        _lightsContainer?.SetAmbientLight(map.AmbientLightColor, null);
        
        var builder = new MapDisplayElementBuilder()
            .WithServices(container, contentManager)
            .WithLightsProvider(_lightsContainer)
            .WithTemplate(gameTemplate)
            .WithMap(map);
        
        _mapDisplayElement = builder.Build();
        
        foreach (var ts in tilesets)
        {
            ts.Dispose();
        }

        var worldBuilder = new AabbSimulationBuilder()
            .WithMap(map)
            .WithMessenger(this)
            .WithFilesProvider(contentManager.FilesProvider)
            .WithContainer(container)
            .WithMaterials(parameters.Materials)
            .WithServicesRegistrar( b =>
            {
                parameters.RegisterServices(b);
                b
                    .WithInstance<IGameInstance>(this).As<IGameRendererFilter>().As<IMessenger>()
                    .WithInstance(_contentCache)
                    .WithInstance(_clock)
                    .WithSingleton<ICamera, Camera>().As<ISoundListener>()
                    .WithSingleton<GameObjectsServices, GameObjectsServices>();
                b.WithSingleton<ICheckpointService, CheckpointService>(new CheckpointService.Parameters
                {
                    MapPath = parameters.MapPath,
                    MapId = mapId,
                    MapPathPrefix = parameters.MapPathPrefix,
                    StorePreview = parameters.StoreCheckpointPreview,
                    PreviewDownSize = parameters.CheckpointPreviewDownSize
                });
            })
            .WithGameTemplate(gameTemplate);
        
        var cacheFilePath = Path.Combine(CacheDirectory, mapId + CacheFileSuffix);
        try
        {
            var fileTime = storage.GetLastModificationTime(cacheFilePath);

            var path =  Assembly.GetEntryAssembly()!.Location;
            var assemblyTime = File.GetLastWriteTime(path);

            if (assemblyTime < fileTime)
            {
                var storedCache = storage.ReadData(cacheFilePath);
                worldBuilder.WithCache(storedCache);
            }
        }
        catch
        {
            // ignored - no cache read if error
        }

        (Simulation, Container, var cache) = worldBuilder.Build();
        
        _camera = Container.Get<ICamera>();
        FixedUpdate += _camera.Update;

        if (parameters.RestoreFrom is not null)
        {
            Container.Get<ICheckpointService>().Restore(parameters.RestoreFrom);
        }

        if (cache?.Length > 0)
        {
            storage.WriteData(cacheFilePath, cache);
        }
    }

    void IGameInstance.Update(float deltaTime)
    {
        var timeElapsed = _internalStopwatch.Elapsed;

        if (timeElapsed - _lastTimeElapsed < TimeSpan.FromMicroseconds(50) 
            && Math.Abs(deltaTime - _lastDeltaTime) < float.Epsilon)
        {
            return;
        }

        _lastDeltaTime = deltaTime;
        _lastTimeElapsed = timeElapsed;
        
        while (_messages.TryDequeue(out var message))
        {
            Message?.Invoke(message);
        }
        _backgroundRenderer?.Update(deltaTime);
        Update(deltaTime);
    }
    
    public TService GetComponent<TService>() where TService : class => Container.Get<TService>();
    
    public void Activate(bool active)
    {
        if (active == _isActive)
            return;
        
        _isActive = active;
        
        foreach (var body in Simulation.Bodies)
        {
            if (body.Owner is IActivationHandler handler)
                handler.Activate(active);
        }
    }

    protected virtual void Render(IRenderer renderer, RectangleF target, float scale)
    {
        if (_isDisposed)
            return;

        if (_lightsContainer is not null)
        {
            _lightsContainer.Reset();

            foreach (var body in Simulation.Bodies)
            {
                if (body.Owner is ILightProvider provider && (FilterMode == 0 || FilterMode == provider.FilterMode))
                {
                    provider.FillLights(_lightsContainer);
                }
            }

            var viewSize = target.Size.ToVector() / scale;
            var visibleBounds = MapRenderer.GetVisibleBounds(_mapDisplayElement, _camera.LookAt,
                new Size((int)MathF.Ceiling(viewSize.X), (int)MathF.Ceiling(viewSize.Y)), _gameTemplate.TileSize);

            var tileSize = _gameTemplate.TileSize;
            _lightsContainer.Apply(new RectangleF(visibleBounds.X * tileSize, visibleBounds.Y * tileSize,
                visibleBounds.Width * tileSize, visibleBounds.Height * tileSize));
        }

        var bgColor = FilterMode == 0 ? GetBgColor() : RgbaColor.Transparent;
        
        renderer.StateManager.SaveState();
        
        if (renderer.CurrentPass == RenderPass.Glow || _backgroundRenderer == null)
        {
            _lightsContainer?.ApplyLights(renderer, _mapDisplayElement.Layers[0].UseAmbientLight, false);
            
            renderer.GeometryRenderer.FillRectangle(target, renderer.CurrentPass == RenderPass.Glow ? RgbaColor.Black : bgColor);
            renderer.LightingManager.EnableLighting(false);
        }

        renderer.StateManager.Translate(target.TopLeft);
        renderer.StateManager.Scale(scale);

        _backgroundRenderer?.Render(renderer);
        
        var size = target.Size.ToVector() / scale;
        var renderSize = new Size((int)MathF.Ceiling(size.X), (int)MathF.Ceiling(size.Y));

        if (_camera is Camera camera)
        {
            camera.VisibleBounds = MapRenderer.GetVisibleBounds(_mapDisplayElement, _camera.LookAt, renderSize, _gameTemplate.TileSize);

            var mainLayerSize = _mapDisplayElement.Layers.First(o => o.IsMain).SourceSize;
            camera.SetMapClampArea(
                new Vector2(mainLayerSize.Width, mainLayerSize.Height),
                renderSize.ToVector() / _gameTemplate.TileSize);
        }
        
        MapRenderer.Render(renderer, _mapDisplayElement, Simulation, _camera.LookAt, 
            renderSize, 
            _gameTemplate, FilterMode);

        renderer.StateManager.RestoreState();
    }

    protected RgbaColor GetBgColor()
    {
        var bgColor = _mapDisplayElement.BackgroundColor;
        if (bgColor.A < 255)
        {
            bgColor = _gameTemplate.GameBackground;
        }
        return bgColor;
    }

    private void RenderDebug(IRenderer renderer, RectangleF target, float scale)
    {
        if (_isDisposed)
            return;

        if (renderer.CurrentPass == RenderPass.Glow)
            return;
        
        renderer.StateManager.SaveState();
        renderer.StateManager.Translate(target.TopLeft);
        renderer.StateManager.Scale(scale);
        
        var size = target.Size.ToVector() / scale;
        MapRenderer.RenderDebug(renderer, _mapDisplayElement, Simulation, _camera.LookAt,
            new Size((int)MathF.Ceiling(size.X), (int)MathF.Ceiling(size.Y)),
            _gameTemplate);
        
        renderer.StateManager.RestoreState();
    }

    private void Update(float deltaTime)
    {
        if (_isDisposed)
            return;

        if (!_isStarted)
        {
            _isStarted = true;
            var list = Simulation.Bodies.Select(o => o.Owner).Where(o => o is not null).ToList();
            foreach(var owner in list)
            { 
                owner.Start();   
            }
        }

        _timer.Update(deltaTime);
        _clock.Update(deltaTime);

        Simulation.SimulationParameters.TimeDelta = _timer.TimeDelta;
        Simulation.Update(deltaTime, FixedUpdate);
        _mapDisplayElement.Update(deltaTime);
        Updated?.Invoke();
    }
    
    public void Dispose()
    {
        if (_isDisposed)
            return;
        
        _isDisposed = true;
        Task.Delay(1000).ContinueWith(_ =>
        {
            _scheduler.Schedule(_mapDisplayElement.Dispose);
            Simulation.Dispose();
            Container?.Dispose();
            _backgroundRenderer?.Dispose();
            _contentCache?.Dispose();
        });
    }
    
    public void PostMessage(object message)
    {
        _messages.Enqueue(message);
    }
}