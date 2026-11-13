using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;
using Ssit.CrossX2.Framework.IO;

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
    private readonly IFileStorage _storage;
    private readonly ICamera _camera;
    private readonly IGameState _gameState;

    public string MapPath { get; }
    
    public CheckpointData LastCheckpoint { get; private set; }

    public CheckpointService(IMapObjects mapObjects, ISimulation simulation, IFileStorage storage, ICamera camera,
        Parameters parameters, IGameState gameState = null)
    {
        _mapObjects = mapObjects;
        _simulation = simulation;
        _storage = storage;
        _camera = camera;
        _gameState = gameState;
        
        MapPath = parameters.MapPath;
    }

    public CheckpointData Capture(int checkpointId)
    {
        var data = new CheckpointData
        {
            MapPath = MapPath,
            CheckpointId = checkpointId,
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

        LastCheckpoint = data;
        return data;
    }

    public CheckpointData Save(int checkpointId)
    {
        var data = Capture(checkpointId);

        Task.Run(() =>
        {
            try
            {
                CheckpointStorage.Write(_storage, data);
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

        // 2. Global game state.
        _gameState?.RestoreFlags(data.Flags ?? Enumerable.Empty<string>());

        // 3. Let objects re-sync with their linked objects.
        foreach (var entry in _mapObjects.All)
        {
            if (!removed.Contains(entry.MapId) && entry.Instance is IStateRestoredHandler handler)
            {
                handler.OnStateRestored();
            }
        }

        _camera.SnapToPrimaryTarget();
        
        LastCheckpoint = data;
    }
}
