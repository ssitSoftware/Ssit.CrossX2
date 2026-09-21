using System.Numerics;

namespace Ssit.CrossX2.Framework.Games.Rendering;

public static class ParticleSystemTools
{
    public static void SpreadParticles(this IParticleSystem particleSystem, int context, int particleGroupId,  int count, Vector2 position, Vector2 direction, Vector2 gravity, float speedMin,
        float speedMax, float timeToLiveMin, float timeToLiveMax, float angle)
    {
        for(var idx = 0;  idx < count; idx++)
        {
            var speed = Random.Shared.NextSingle() * (speedMax - speedMin) + speedMin;
            var timeToLive = Random.Shared.NextSingle() * (timeToLiveMax - timeToLiveMin) + timeToLiveMin;
            
            var angleToRotate = Random.Shared.NextSingle() * angle - angle / 2f;
            var dir = Vector2.Transform(direction, Matrix3x2.CreateRotation(angleToRotate));
            
            particleSystem.AddParticle(context, particleGroupId, position, dir, gravity, speed, timeToLive);
        }
    }
}