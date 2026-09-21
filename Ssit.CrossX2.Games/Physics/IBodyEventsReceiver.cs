using System.Numerics;

namespace Ssit.CrossX2.Framework.Games.Physics;

public interface IBodyEventsReceiver
{
    void OnBodyUpdated() {}
    void OnBodyMoved(Vector2 offset) {}
    void OnBodyDisposed() {}
    void OnFriction(ref float frictionVelocityX, ref float frictionVelocityY) {}
    void OnCollision(ICollider source, ICollider other, Vector2 impact) {}
}