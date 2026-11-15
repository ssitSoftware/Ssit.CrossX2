using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;
using Ssit.CrossX2.Framework.IoC;

namespace Ssit.CrossX2.Framework.Games.Logic.Checkpoints;

public class CheckpointService : ICheckpointService
{
    public class Parameters
    {
        public string MapPath { get; init; }
    }

    private static readonly JsonSerializer Serializer = JsonSerializer.CreateDefault();
    
    private readonly IMapObjects _mapObjects;
    private readonly ISimulation _simulation;
    private readonly CheckpointStorage _storage;
    private readonly ICamera _camera;
    private readonly IGameState _gameState;
    private readonly IIoCContainer _container;

    public string MapPath { get; }
    
    public CheckpointData LastCheckpoint { get; private set; }

    public CheckpointService(IMapObjects mapObjects, ISimulation simulation, CheckpointStorage storage, ICamera camera,
        IIoCContainer container, Parameters parameters, IGameState gameState = null)
    {
        _container = container;
        _mapObjects = mapObjects;
        _simulation = simulation;
        _storage = storage;
        _camera = camera;
        _gameState = gameState;
        
        MapPath = parameters.MapPath;
    }

    public CheckpointData Capture()
    {
        var data = new CheckpointData
        {
            MapPath = MapPath,
            SavedAtUtc = DateTime.UtcNow,
            Flags = _gameState?.Flags.ToList() ?? new List<string>()
        };

        var bodies = _simulation.Bodies.ToHashSet();

        foreach (var entry in _mapObjects.All)
        {
            if (entry.Instance is IBodyOwner owner && !bodies.Contains(owner.Body))
            {
                data.Objects[entry.MapId] = new ObjectSnapshot { TypeId = entry.TypeId, Removed = true };
                continue;
            }

            if (entry.Instance is not IStatefulObject stateful)
                continue;

            var state = stateful.CaptureState();
            if (state is null)
                continue;

            data.Objects[entry.MapId] = new ObjectSnapshot
            {
                TypeId = entry.TypeId,
                State = JToken.FromObject(state, Serializer)
            };
        }

        CaptureSpawnedObjects(data);

        LastCheckpoint = data;
        return data;
    }

    /// <summary>
    /// Stores stateful objects that are in the simulation but weren't created from the map.
    /// </summary>
    private void CaptureSpawnedObjects(CheckpointData data)
    {
        var mapInstances = _mapObjects.All.Select(o => o.Instance).ToHashSet(ReferenceEqualityComparer.Instance);

        foreach (var body in _simulation.Bodies)
        {
            if (body.Owner is not IStatefulObject stateful || mapInstances.Contains(stateful))
                continue;

            var state = stateful.CaptureState();
            if (state is null)
                continue;

            data.SpawnedObjects.Add(new SpawnedObjectSnapshot
            {
                ObjectType = TypeName(stateful.GetType()),
                StateType = TypeName(state.GetType()),
                State = JToken.FromObject(state, Serializer)
            });
        }
    }

    public CheckpointData Save(string fileName)
    {
        var data = Capture();

        Task.Run(() =>
        {
            try
            {
                _storage.Write(fileName, data);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to write checkpoint: {e}");
            }
        });
        
        return data;
    }

    public void Restore(CheckpointData data)
    {
        if (data is null)
            return;

        if (!string.Equals(data.MapPath, MapPath, StringComparison.Ordinal))
        {
            Console.WriteLine($"Checkpoint map '{data.MapPath}' doesn't match the loaded map '{MapPath}' - not restored.");
            return;
        }

        var removed = new HashSet<int>();
        
        // 1. Remove objects that no longer existed and restore states of the others.
        foreach (var entry in _mapObjects.All)
        {
            if (!data.Objects.TryGetValue(entry.MapId, out var snapshot))
                continue;

            if (snapshot.TypeId != entry.TypeId)
                continue;

            if (snapshot.Removed)
            {
                if (entry.Instance is IBodyOwner owner)
                {
                    _simulation.RemoveBody(owner.Body);
                    removed.Add(entry.MapId);
                }
                continue;
            }

            if (snapshot.State is null || entry.Instance is not IStatefulObject stateful)
                continue;

            try
            {
                var state = snapshot.State.ToObject(stateful.StateType, Serializer);
                stateful.RestoreState(state);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to restore state of map object {entry.MapId} ({entry.TypeId}): {e}");
            }
        }

        // 2. Objects created at runtime.
        var spawned = RestoreSpawnedObjects(data.SpawnedObjects);

        // 3. Global game state.
        _gameState?.RestoreFlags(data.Flags ?? Enumerable.Empty<string>());

        // 4. Let objects re-sync with their linked objects.
        foreach (var entry in _mapObjects.All)
        {
            if (!removed.Contains(entry.MapId) && entry.Instance is IStateRestoredHandler handler)
            {
                handler.OnStateRestored();
            }
        }

        foreach (var obj in spawned)
        {
            (obj as IStateRestoredHandler)?.OnStateRestored();
        }

        _camera.SnapToPrimaryTarget();
        
        LastCheckpoint = data;
    }

    /// <summary>
    /// Recreates runtime objects: constructs each through the IoC container with its state object as the creation
    /// parameter (so the type needs a public constructor taking its state) and calls RestoreState.
    /// </summary>
    private List<IStatefulObject> RestoreSpawnedObjects(List<SpawnedObjectSnapshot> snapshots)
    {
        var restored = new List<IStatefulObject>();
        
        if (snapshots is null)
            return restored;

        foreach (var snapshot in snapshots)
        {
            try
            {
                var type = Type.GetType(snapshot.ObjectType);
                var stateType = Type.GetType(snapshot.StateType ?? "");
                
                if (type is null || stateType is null || !typeof(IStatefulObject).IsAssignableFrom(type) || snapshot.State is null)
                {
                    Console.WriteLine($"Can't restore spawned object of type '{snapshot.ObjectType}'.");
                    continue;
                }

                var state = snapshot.State.ToObject(stateType, Serializer);
                
                var obj = (IStatefulObject)_container.IoCConstruct(type, state);
                obj.RestoreState(state);
                restored.Add(obj);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to restore spawned object of type '{snapshot.ObjectType}': {e}");
            }
        }

        return restored;
    }

    private static string TypeName(Type type) => $"{type.FullName}, {type.Assembly.GetName().Name}";
}
