using System.Diagnostics;
using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.Games.Logic.Objects;
using Ssit.CrossX2.Framework.Games.Map;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;
using Ssit.CrossX2.Framework.Games.Rendering.Map;
using Ssit.CrossX2.Framework.Games.Template;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.IO;
using Ssit.CrossX2.Framework.IoC;
using Ssit.CrossX2.Framework.Services;

namespace Ssit.CrossX2.Framework.Games.Logic;

public class AabbGameInstance : IGameInstance, IMessenger
{
    public interface IBackgroundRenderer: IDisposable
    {
        void Render(IRenderer renderer);
        void Update(float deltaTime);
    }
    
    public class Parameters
    {
        public string MapPath { get; set; }
        public Action<IIoCContainerBuilder> RegisterServices { get; set; }
        public IMaterial[] Materials { get; set; }
        public int BackgroundColorIndex { get; set; }
        public int? MaxFps { get; set; }
        public int? TargetFps { get; set; }
        public IBackgroundRenderer BackgroundRenderer { get; set; }
    }
    
    IIoCContainer IGameInstance.Services => Container;
    
    public event Action<float> FixedUpdate;
    public event Action Updated;
    public event Action<object> Message;
    public IMessenger Messenger => this;
    
    private readonly IGameTimer _timer;

    private readonly IActionScheduler _scheduler;
    private readonly IGameTemplate _gameTemplate;
    private readonly MapDisplayElement _mapDisplayElement;
    
    private readonly IBackgroundRenderer _backgroundRenderer;
    
    // ReSharper disable once MemberCanBePrivate.Global
    public readonly ISimulation Simulation;
    public readonly IIoCContainer Container;
    private readonly ICamera _camera;
    
    private bool _isDisposed;

    private int _bgColorIndex = 0;
    private Queue<object> _messages = new();

    int IGameInstance.RenderPasses => 1;
    
    private readonly Stopwatch _internalStopwatch = new();
    private TimeSpan _lastTimeElapsed = TimeSpan.Zero;
    private float _lastDeltaTime = 0;
    
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
        if (parameters.TargetFps.HasValue)
        {
            _timer = container.IoCConstruct<TargetGameTimer>(new TargetGameTimer.Parameters{ TargetFps = parameters.TargetFps.Value});
        }
        else
        {
            _timer = new GameTimer(parameters.MaxFps.HasValue ? 1f / parameters.MaxFps.Value : 0f);
        }

        _internalStopwatch.Start();
        
        _scheduler = scheduler;
        _gameTemplate = gameTemplate;
        _bgColorIndex = parameters.BackgroundColorIndex;
        _backgroundRenderer = parameters.BackgroundRenderer;
        
        using var stream = contentManager.FilesProvider.Open(parameters.MapPath);
        var map = MapFile.FromStream(stream, gameTemplate);

        var tilesets = map.Tilesets.Select(contentManager.Get<ITexture>).ToArray();
        
        var builder = new MapDisplayElementBuilder()
            .WithServices(container, contentManager)
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
                    .WithInstance<IGameInstance>(this)
                    .WithSingleton<ICamera, Camera>()
                    .WithSingleton<GameObjectsServices, GameObjectsServices>();
            })
            .WithGameTemplate(gameTemplate);
        
        var cacheFilePath = parameters.MapPath.Replace('\\', '_').Replace('/', '_').Replace(':', '_');
        try
        {
            var storedCache = storage.ReadData(cacheFilePath);
            worldBuilder.WithCache(storedCache);
        }
        catch
        {
            // ignored - no cache read if error
        }

        (Simulation, Container, var cache) = worldBuilder.Build();
        
        _camera = Container.Get<ICamera>();
        FixedUpdate += _camera.Update;

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

        var bgColor = GetBgColor();
        
        renderer.StateManager.SaveState();

        if (renderer.CurrentPass == RenderPass.Glow || _backgroundRenderer == null)
        {
            renderer.GeometryRenderer.FillRectangle(target, renderer.CurrentPass == RenderPass.Glow ? RgbaColor.Black : bgColor);
        }

        renderer.StateManager.Translate(target.TopLeft);
        renderer.StateManager.Scale(scale);

        _backgroundRenderer?.Render(renderer);
        
        var size = target.Size.ToVector() / scale;
        
        MapRenderer.Render(renderer, _mapDisplayElement, Simulation, _camera.LookAt,
            new Size((int)MathF.Ceiling(size.X), (int)MathF.Ceiling(size.Y)),
            _gameTemplate.TileSize);

        renderer.StateManager.RestoreState();
    }

    protected RgbaColor GetBgColor()
    {
        var bgColor = _mapDisplayElement.BackgroundColor;
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

        _timer.Update(deltaTime);
        
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
        });
    }
    
    public void PostMessage(object message)
    {
        _messages.Enqueue(message);
    }
}