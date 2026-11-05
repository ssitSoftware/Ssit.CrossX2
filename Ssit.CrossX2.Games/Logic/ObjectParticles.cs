using System.Numerics;
using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Rendering;
using Ssit.CrossX2.Framework.Games.Template;
using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.Games.Logic;

public class ObjectParticles<TParent> : IGameObjectRenderer, IBodyOwner where  TParent: IGameObjectRenderer, IBodyOwner
{
    private readonly IGameTemplate _gameTemplate;

    public record Parameters(TParent Parent, string TexturePath, Size ElementSize);

    private class Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        
        public int Index { get; init; }
        public Vector2 Gravity { private get; init; }

        public float MaxScale { private get; init; }
        public float MinScale { private get; init; }
        public float EndOpacity { private get; init; }
        
        public float TotalTimeToLive { private get; init;}

        private float _scale;
        private float _opacity;

        private float _timeToLive;

        public bool IsFinished => _timeToLive <= 0;
        
        public float Scale => _scale;
        public float Opacity => _opacity;
        
        public Particle Prepare()
        {
            _timeToLive = TotalTimeToLive;
            Update(0);
            return this;
        }

        public void Update(float dt)
        {
            Velocity += Gravity * dt;
            Position += Velocity * dt;
            _timeToLive -= dt;

            _opacity = _timeToLive / TotalTimeToLive * (1 - EndOpacity) + EndOpacity;
            _scale = _timeToLive / TotalTimeToLive * (MaxScale - MinScale) + MinScale;
        }
    }
    
    private readonly TParent _parent;
    private RectangleF _bounds;
    private readonly IBody _body;
    private readonly ResourceHandle<ITexture> _texture;
    private readonly Size _elementSize;
    
    RectangleF IGameObjectRenderer.Bounds => _bounds;
    IBody IBodyOwner.Body => _body;
    int IGameObjectRenderer.ZOrder => _parent.ZOrder + 1;
    
    public int Count { get; }
    private readonly int _columns;
    
    private readonly List<Particle> _particles = new();
    
    public ObjectParticles(Parameters parameters, IContentManager contentManager, IGameTemplate gameTemplate)
    {
        _gameTemplate = gameTemplate;
        _parent = parameters.Parent;
        _texture =  contentManager.Get<ITexture>(parameters.TexturePath);
        _elementSize = parameters.ElementSize;
        
        var rc = _texture.Resource.Size.ToVector() / _elementSize.ToVector();
        _columns = (int)Math.Ceiling(rc.X);
        Count = _columns * (int)Math.Ceiling(rc.Y);
        
        _body = _parent.Body.Simulation.CreateBody(this);
        _body.IsKinematic = true;
        _body.Position = Vector2.Zero;
        _body.Mass = 1000000;
    }

    public void Spawn(int index, Vector2 position, Vector2 velocity, float timeToLive, float endOpacity = 1, float scale = 1, float minScale = 1, Vector2? gravity = null)
    {
        _particles.Add(new Particle
        {
            Index = index,
            Position = position,
            Velocity = velocity,
            TotalTimeToLive = timeToLive,
            EndOpacity = endOpacity,
            MaxScale = scale,
            MinScale = minScale,
            Gravity = gravity ?? _body.Simulation.SimulationParameters.GravityAcceleration,
        }.Prepare());
    }
    
    void IDisposable.Dispose()
    {
    }

    void IBodyOwner.OnFixedUpdate(out bool cancelUpdate)
    {
        cancelUpdate = false;
        
        _bounds = (RectangleF)_body.Simulation.Bounds;

        for (var idx = 0; idx < _particles.Count;)
        {
            var particle = _particles[idx];
            particle.Update(_body.Simulation.SimulationParameters.TimeDelta);

            if (particle.IsFinished)
            {
                _particles.RemoveAt(idx);
                continue;
            }
            ++idx;
        }
    }
    
    void IGameObjectRenderer.Render(IRenderer renderer, RgbaColor color, float depth)
    {
        foreach (var particle in _particles)
        {
            var position = particle.Position;
            position *= _gameTemplate.TileSize;

            var x = particle.Index % _columns;
            var y =  particle.Index / _columns;

            x *= _elementSize.Width;
            y *= _elementSize.Height;
            
            var src = new RectangleF(x, y, _elementSize.Width, _elementSize.Height);
            
            renderer.SpriteRenderer.Draw(_texture.Resource, position, src, _elementSize.ToVector() / 2, scale: particle.Scale, color: RgbaColor.White * particle.Opacity);
        }
    }
}