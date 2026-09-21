using System.Numerics;
using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.Games.Rendering;

public interface IParticleSystem
{
    int RequestContextId();
    
    void Draw(IRenderer renderer, int context);
    
    void AddParticle(int context, int particleGroupId, Vector2 position, Vector2 direction,  Vector2 gravity, float speed, float timeToLive);
    IParticleSystem RegisterParticleGroup(int id, string image, Size size, float minScale);
}