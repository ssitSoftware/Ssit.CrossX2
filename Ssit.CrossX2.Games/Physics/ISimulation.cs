using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.Games.Physics.Coliders;

namespace Ssit.CrossX2.Framework.Games.Physics;

public interface ISimulation: IDisposable
{
    float ActiveTime { get; }
    IMessenger Messenger { get; }
    SimulationParameters SimulationParameters { get; }
    float MovementEpsilon { get; }
    event Action Disposed;
    
    Aabb Bounds { get; }

    IReadOnlyList<IBody> Bodies { get; }
     
    void Update(float timeInSeconds, Action<float> onFixedUpdate);
    IBody CreateBody();
    IBody CreateBody(IBodyOwner owner);
    void RemoveBody(IBody body);
    void InitializeStaticColliders(Aabb bounds, IEnumerable<ICollider> colliders);
    bool CheckCollision(Aabb aabb, IBody testingBody, int colliderGroup = IMaterial.AllColliders, float epsilon = 0, IList<ICollider> colliders = null, ColliderType colliderType = ColliderType.Static | ColliderType.Dynamic, bool debugRegister = false);
    void GetColliders(Aabb bounds, IList<ICollider> colliders);
    IReadOnlyList<ICollider> GetColliders(Aabb bounds);

    IReadOnlyList<ICollider> GetColliders(Aabb aabb, IBody testingBody, int colliderGroup, float epsilon = 0,
        ColliderType colliderType = ColliderType.Static | ColliderType.Dynamic, bool debugRegister = false);
    
    IReadOnlyList<Aabb> Debug_GetCollisionChecks();
    void Debug_GetQuadTreeAreas(IList<Aabb> aabbs);
    void RemoveCollider(ICollider collider);
    void AddCollider(ICollider collider);
    ICollider CreateCollider<TCreationParameters>(TCreationParameters creationParameters) where TCreationParameters : ColliderCreationParameters;
}