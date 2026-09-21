using System.Numerics;

namespace Ssit.CrossX2.Framework.Games.Physics;

public enum KinematicMoveMode
{
    Kinematic,
    Hybrid,
    MoveAddVelocity,
    Move
}

public interface IBody: IDisposable
{
    float Mass { get; set; }
    bool IsActive { get; }
    bool IsKinematic { get; set; }
    Vector2 Position { get; set; }
    Vector2 Velocity { get; set; }
    Vector2 KinematicVelocity { get; set; }
    ICollider[] Colliders { get; }
    IBodyOwner Owner { get; }
    ISimulation Simulation { get; }
    float StepUpTolerance { get; set; }
    int UpdateOrder { get; set; }

    void AddColliders(params ICollider[] colliders);
    void RemoveCollider(ICollider collider);
    void Move(Vector2 offset);
    void KinematicMove(Vector2 offset, KinematicMoveMode mode, IBody skipBody = null);
    void ApplyForce(Vector2 force);
    void LimitVelocity(float maxHorizontalVelocity, float maxVerticalVelocity);
    void LimitVelocity(float maxVelocity);
    void Touch();
    void DetachFromSimulation();
    void ReattachToSimulation();
    
    void AddEventsReceiver(IBodyEventsReceiver receiver);
    void RemoveEventsReceiver(IBodyEventsReceiver receiver);
    
    ICollider FindCollider(string name);
}