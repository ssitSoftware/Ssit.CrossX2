using System.Numerics;
using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.Games.Algorithms;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Physics.Coliders;

namespace Ssit.CrossX2.Framework.Games.AabbPhysics;

internal class Simulation : ISimulation
{
    private double _timeToUpdate;

    public float MovementEpsilon => MovementCollisionCalculator.MovementEpsilon;
    
    public float ActiveTime { get; private set; }
    public IMessenger Messenger { get; internal set; }

    public SimulationParameters SimulationParameters { get; } = new()
    {
        GravityAcceleration = new Vector2(0, 10),
        MaxHorizontalSpeed = 1000,
        MaxVerticalSpeed = 1000,
        TimeDelta = 0.01f
    };

    public Aabb Bounds => _collidersRootNode.Aabb;

    public event Action Disposed;
    
    private readonly List<Body> _bodies = new();
    private readonly List<Body> _detachedBodies = new();
    
    private QuadTreeNode<ICollider> _collidersRootNode;
    private readonly List<ICollider> _collidersToTest = new();

    private readonly List<Body> _bodiesToRemove = new();
    
    public IReadOnlyList<IBody> Bodies => _bodies;

    private readonly List<ICollider> _tempCollidersList = new();

    private readonly List<Aabb> _collisionChecks = new();

    private bool _bodiesChanged;
    
    public IReadOnlyList<ICollider> GetColliders(Aabb bounds)
    {
        _tempCollidersList.Clear();
        GetColliders(bounds, _tempCollidersList);
        return _tempCollidersList;
    }

    public void Debug_GetQuadTreeAreas(IList<Aabb> aabbs) => _collidersRootNode.Debug_GetNodesAabbs(aabbs);

    public IReadOnlyList<Aabb> Debug_GetCollisionChecks() => _collisionChecks;

    public ICollider CreateCollider<TCreationParameters>(TCreationParameters creationParameters) where TCreationParameters: ColliderCreationParameters
        => CollidersFactory.Create(creationParameters);

    public void GetColliders(Aabb bounds, IList<ICollider> colliders) => _collidersRootNode.GetElements(bounds, colliders);

    public IBody CreateBody()
    {
        var body = new Body(this);
        _bodies.Add(body);
        _bodiesChanged = true;
        return body;
    }

    public IBody CreateBody(IBodyOwner owner)
    {
        var body = new Body(this, owner);
        _bodies.Add(body);
        _bodiesChanged = true;
        return body;
    }

    public void AttachBody(Body body)
    {
        _detachedBodies.Remove(body);
        if (_bodies.Contains(body)) return;
        _bodiesChanged = true;
        _bodies.Add(body);
    }

    public void RemoveBody(IBody body)
    {
        _bodiesToRemove.Add((Body)body);
        _bodiesChanged = true;
    }

    public void DetachBody(Body body, bool keepForLater)
    {
        _bodies.Remove(body);
        if(keepForLater)
        {
            _detachedBodies.Add(body);
        }
        
        foreach (var collider in body.Colliders ?? [])
        {
            _collidersRootNode.RemoveElement(collider);
        }
        
        _bodiesChanged = true;
    }

    public void SortBodies()
    {
        _bodies.Sort((o1, o2) => o1.UpdateOrder - o2.UpdateOrder);
    }
    
    public void GetColliders(IList<ICollider> list, Aabb aabb, IBody testingBody, int colliderGroup, float epsilon = 0, ColliderType colliderType = ColliderType.Static | ColliderType.Dynamic, bool debugRegister = false)
    {
        list.Clear();
        CheckCollision(aabb, testingBody, colliderGroup, epsilon, list, colliderType, debugRegister);
    }
    
    public IReadOnlyList<ICollider> GetColliders(Aabb aabb, IBody testingBody, int colliderGroup, float epsilon = 0, ColliderType colliderType = ColliderType.Static | ColliderType.Dynamic, bool debugRegister = false)
    {
        _tempCollidersList.Clear();
        CheckCollision(aabb, testingBody, colliderGroup, epsilon, _tempCollidersList, colliderType, debugRegister);
        return _tempCollidersList;
    }

    public bool CheckCollision(Aabb aabb, IBody testingBody, int colliderGroup, float epsilon = 0, IList<ICollider> colliders = null, ColliderType colliderType = ColliderType.Static | ColliderType.Dynamic, bool registerDebug = false)
    {
        _collidersToTest.Clear();
        _collidersRootNode.GetElements(aabb, _collidersToTest);

        for (var idx = 0; idx < _collidersToTest.Count; ++idx)
        {
            var collider = _collidersToTest[idx];

            if (!collider.IsActive) continue;
            if (collider.AttachedBody == testingBody) continue;
            if ((colliderType & collider.Type) == 0) continue;
            if( (collider.Material.ColliderGroup & colliderGroup) == 0) continue;

            var colliderAabb = collider.Aabb;
            if (!colliderAabb.Intersects(aabb, epsilon)) continue;

            if (colliders == null) return true;
            colliders.Add(collider);
        }

        if (registerDebug)
        {
            _collisionChecks.Add(aabb);
        }
        
        return colliders?.Count > 0;
    }

    public void UpdateColliderInTree(ICollider collider, bool remove = false)
    {
        _collidersRootNode.RemoveElement(collider);
        if (collider.Type.HasFlag(ColliderType.Particle)) return;
        if (remove) return;
        
        _collidersRootNode.AddElement(collider);
    }

    public void InitializeStaticColliders(Aabb bounds, IEnumerable<ICollider> colliders)
    {
        var maxElementsPerNode = 8;
        _collidersRootNode = new QuadTreeNode<ICollider>(bounds, colliders, maxElementsPerNode);
    }

    public void AddCollider(ICollider collider)
    {
        _collidersRootNode.AddElement(collider);
    }
    
    public void RemoveCollider(ICollider collider)
    {
        if(collider.AttachedBody != null) throw  new InvalidOperationException("Cannot remove collider from a non-static collider");
        _collidersRootNode.RemoveElement(collider);
    }

    public void Update(float timeInSeconds, Action<float> onFixedUpdate)
    {
        _collisionChecks.Clear();
        ActiveTime += timeInSeconds;
        
        if (_bodiesChanged)
        {
            SortBodies();
            _bodiesChanged = false;
        }
        OnUpdate(timeInSeconds);

        _timeToUpdate += timeInSeconds;

        RemoveBodies();
        while (_timeToUpdate >= SimulationParameters.TimeDelta)
        {
            OnFixedUpdate();
            onFixedUpdate?.Invoke(SimulationParameters.TimeDelta);
            _timeToUpdate -= SimulationParameters.TimeDelta;
            RemoveBodies();
            
            if (_bodiesChanged)
            {
                SortBodies();
                _bodiesChanged = false;
            }
        }

        OnPostUpdate();
    }

    private void OnPostUpdate()
    {
        for (var idx = 0; idx < _bodies.Count; ++idx)
        {
            _bodies[idx].PostUpdate();
        }
    }

    private void RemoveBodies()
    {
        foreach (var body in _bodiesToRemove)
        {
            body.Dispose();
        }
        _bodiesToRemove.Clear();
    }

    protected virtual void OnUpdate(float time)
    {
        for (var idx = 0; idx < _bodies.Count; ++idx)
        {
            _bodies[idx].Update(time);
        }
    }

    protected virtual void OnFixedUpdate()
    {
        var dt = SimulationParameters.TimeDelta;
        for (var idx = 0; idx < _bodies.Count; ++idx)
        {
            _bodies[idx].FixedUpdate(dt);
        }

        for (var idx = 0; idx < _bodies.Count; ++idx)
        {
            _bodies[idx].PostFixedUpdate();
        }
    }

    public void Dispose()
    {
        var objects = _bodies.ToList();
        foreach (var obj in objects)
        {
            obj.Dispose();
        }

        objects = _detachedBodies.ToList();
        foreach (var obj in objects)
        {
            obj.Dispose();
        }
        _bodies.Clear();
        _detachedBodies.Clear();
        
        Disposed?.Invoke();
    }
}