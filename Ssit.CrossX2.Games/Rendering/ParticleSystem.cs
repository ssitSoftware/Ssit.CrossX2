using System.Numerics;
using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Template;
using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.Games.Rendering;

public class ParticleSystem(IContentManager contentManager, IGameTemplate gameTemplate) : IParticleSystem, IUpdatable, IBodyOwner
{
    private class ParticleInstance
    {
        public ITexture Texture;
        public Rectangle Source;
        public Vector2 Position;
        public Vector2 Velocity;
        public float TimeToLive;
        public float TimeToLiveOriginal;
        public int Context;

        public float MinScale;
        
        public Vector2 Gravity;

        public void Draw(IRenderer renderer, IGameTemplate gameTemplate)
        {
            var pos = Position * gameTemplate.TileSize;

            renderer.SpriteRenderer.Draw(Texture, pos, Source, new Vector2(Source.Width / 2f, Source.Height / 2f), imageTransform: Velocity.X < 0 ? ImageTransform.FlipHorizontal : ImageTransform.None,
                scale: MinScale + TimeToLive / TimeToLiveOriginal * (1-MinScale));
        }
    }

    private readonly Dictionary<int, (ResourceHandle<ITexture>, Size, float)> _particleGroups = new();
    
    private readonly List<ParticleInstance> _particles = new();
    private readonly List<ParticleInstance> _deadParticles = new();

    public IBody Body => _body;
    public event Action FixedUpdate;
    
    private IBody _body;
    private int _nextId = 1;
    
    public int RequestContextId()
    {
        return _nextId++;
    }

    public void Draw(IRenderer renderer, int context)
    {
        for (var idx =0; idx < _particles.Count; idx++)
        {
            var particle = _particles[idx];
            if (particle.Context != context)
            {
                continue;
            }

            particle.Draw(renderer, gameTemplate);
        }
    }
    
    public void OnFixedUpdate(out bool cancelUpdate)
    {
        cancelUpdate = false;
        
        var dt = Body.Simulation.SimulationParameters.TimeDelta;
        
        for (var idx = 0; idx < _particles.Count; )
        {
            var particle = _particles[idx];
            particle.Velocity += particle.Gravity * dt;
            particle.Position += particle.Velocity * dt;
            particle.TimeToLive -= dt;
            
            if (particle.TimeToLive <= 0)
            {
                _particles.RemoveAt(idx);
                _deadParticles.Add(particle);
                continue;
            }

            idx++;
        }
        
        FixedUpdate?.Invoke();
    }

    public void AddParticle(int context, int particleGroupId, Vector2 position, Vector2 direction, Vector2 gravity, float speed, float timeToLive)
    {
        var particle = CreateParticle();
        particle.Context = context;
        particle.Position = position;
        particle.Velocity = direction * speed;
        particle.TimeToLive = timeToLive;
        particle.TimeToLiveOriginal = timeToLive;
        particle.Gravity = gravity;
        
        var (handle, size, minScale) = _particleGroups[particleGroupId];
        
        particle.MinScale = minScale;
        particle.Texture = handle.Resource;

        var index = Random.Shared.Next(0, handle.Resource.Size.Width / size.Width);
        particle.Source = new Rectangle(index * size.Width, 0, size.Width, size.Height);
        
        _particles.Add(particle);
    }

    public IParticleSystem RegisterParticleGroup(int id, string image, Size size, float minScale)
    {
        var handle = contentManager.Get<ITexture>(image);
        _particleGroups.Add(id, (handle, size, minScale));
        return this;
    }

    public void Dispose()
    {
        foreach(var group in _particleGroups)
        {
            group.Value.Item1.Dispose();
        }

        _particleGroups.Clear();
    }

    private ParticleInstance CreateParticle()
    {
        ParticleInstance particle;
        if (_deadParticles.Count > 0)
        {
            particle = _deadParticles[^1];
            _deadParticles.RemoveAt(_deadParticles.Count - 1);
        }
        else
        {
            particle = new ParticleInstance();
        }
        return particle;
    }

    public void Attach(ISimulation simulation)
    {
        _body = simulation.CreateBody(this);
    }
}