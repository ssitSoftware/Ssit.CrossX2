using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.Games.Map;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.IoC;

namespace Ssit.CrossX2.Framework.Games.Logic.Checkpoints;

public class CheckpointService : ICheckpointService
{
    public class Parameters
    {
        public string MapPath { get; init; }

        /// <summary>
        /// Id of the map naming its state file in the slot directory (<see cref="Map.MapPathHelper.MapId"/>).
        /// </summary>
        public string MapId { get; init; }

        /// <summary>
        /// Common prefix of the map paths, cut off to get the map ids (<see cref="Map.MapPathHelper.MapId"/>).
        /// Used to get the id of the map the game continues on when saving with a target map.
        /// </summary>
        public string MapPathPrefix { get; init; }

        /// <summary>
        /// Store a JPEG preview of the last rendered frame in each checkpoint (<see cref="CheckpointData.Preview"/>).
        /// </summary>
        public bool StorePreview { get; init; }

        /// <summary>
        /// How many times the preview is smaller than the rendered frame.
        /// </summary>
        public float PreviewDownSize { get; init; } = 4;
    }

    private static readonly JsonSerializer Serializer = JsonSerializer.CreateDefault();
    
    private readonly IMapObjects _mapObjects;
    private readonly ISimulation _simulation;
    private readonly CheckpointStorage _storage;
    private readonly ICamera _camera;
    private readonly IGameState _gameState;
    private readonly IGameClock _clock;
    private readonly IIoCContainer _container;
    private readonly IRenderHost _renderHost;
    private readonly ISnapshotService _snapshotService;
    private readonly bool _storePreview;
    private readonly float _previewDownSize;
    private readonly string _mapPathPrefix;

    public string MapPath { get; }
    
    public string MapId { get; }
    
    public CheckpointData LastCheckpoint { get; private set; }

    public CheckpointService(IMapObjects mapObjects, ISimulation simulation, CheckpointStorage storage, ICamera camera,
        IIoCContainer container, Parameters parameters, IGameState gameState = null, IGameClock clock = null,
        IRenderHost renderHost = null, ISnapshotService snapshotService = null)
    {
        _clock = clock;
        _renderHost = renderHost;
        _snapshotService = snapshotService;
        _storePreview = parameters.StorePreview;
        _previewDownSize = Math.Max(1f, parameters.PreviewDownSize);

        _container = container;
        _mapObjects = mapObjects;
        _simulation = simulation;
        _storage = storage;
        _camera = camera;
        _gameState = gameState;
        
        MapPath = parameters.MapPath;
        MapId = parameters.MapId;
        _mapPathPrefix = parameters.MapPathPrefix;
    }

    public CheckpointData Capture(object playerState, string targetMapPath = null) =>
        Capture(playerState, targetMapPath, true);

    private CheckpointData Capture(object playerState, string targetMapPath, bool preview)
    {
        var savedAtUtc = DateTime.UtcNow;
        
        // The game continues on the target map (if any) - the map state still belongs to this map.
        var gameMapPath = string.IsNullOrEmpty(targetMapPath) ? MapPath : targetMapPath;
        var gameMapId = string.Equals(gameMapPath, MapPath, StringComparison.Ordinal)
            ? MapId
            : MapPathHelper.MapId(gameMapPath, _mapPathPrefix);
        
        var mapState = new MapStateData
        {
            MapPath = MapPath,
            MapId = MapId,
            SavedAtUtc = savedAtUtc
        };

        var bodies = _simulation.Bodies.ToHashSet();

        foreach (var entry in _mapObjects.All)
        {
            if (entry.Instance is IBodyOwner owner && !bodies.Contains(owner.Body))
            {
                mapState.Objects[entry.MapId] = new ObjectSnapshot { TypeId = entry.TypeId, Removed = true };
                continue;
            }

            if (entry.Instance is not IStatefulObject stateful)
                continue;

            var state = stateful.CaptureState();
            if (state is null)
                continue;

            mapState.Objects[entry.MapId] = new ObjectSnapshot
            {
                TypeId = entry.TypeId,
                State = JToken.FromObject(state, Serializer)
            };
        }

        CaptureSpawnedObjects(mapState);

        var gameState = new GameStateData
        {
            MapPath = gameMapPath,
            MapId = gameMapId,
            SavedAtUtc = savedAtUtc,
            Time = _clock?.Time ?? 0,
            Flags = _gameState?.Flags.ToList() ?? new List<string>(),
            PlayerState = playerState is null ? null : JToken.FromObject(playerState, Serializer)
        };

        var data = new CheckpointData(gameState, mapState, preview ? CapturePreview() : null);
        
        LastCheckpoint = data;
        return data;
    }

    /// <summary>
    /// Takes a JPEG snapshot of the last rendered frame (before CRT simulation and post rendering).
    /// Returns null if previews are disabled or the snapshot fails - the checkpoint is saved without it.
    /// </summary>
    private byte[] CapturePreview()
    {
        if (!_storePreview || _snapshotService is null || _renderHost?.SceneTexture is not { } texture)
            return null;

        try
        {
            using var stream = new MemoryStream();
            _snapshotService.GetSnapshot(stream, texture, _previewDownSize);
            return stream.ToArray();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to take checkpoint preview: {e}");
            return null;
        }
    }

    /// <summary>
    /// Stores stateful objects that are in the simulation but weren't created from the map.
    /// </summary>
    private void CaptureSpawnedObjects(MapStateData data)
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

    public CheckpointData Save(int slot, object playerState)
    {
        var data = Capture(playerState, null, true);
        _storage.WriteAsync(slot, data);
        return data;
    }

    public CheckpointData SaveImmediate(int slot, object playerState, string targetMapPath = null)
    {
        var data = Capture(playerState, targetMapPath, false);
        _storage.WriteImmediateAsync(slot, data);
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
        var spawned = new List<IStatefulObject>();

        // 0. Game time first - objects may store times relative to it (e.g. time of death).
        _clock?.Set(data.GameState.Time);

        // 1. + 2. Map objects - only if the map has a stored state.
        if (data.MapState is { } mapState && string.Equals(mapState.MapPath, MapPath, StringComparison.Ordinal))
        {
            RestoreMapObjects(mapState, removed);
            spawned = RestoreSpawnedObjects(mapState.SpawnedObjects);
        }

        // 3. Global game state.
        _gameState?.RestoreFlags(data.GameState.Flags ?? Enumerable.Empty<string>());

        // 4. Player state - kept in the game state, not in the map state.
        RestorePlayerState(data.GameState.PlayerState);

        // 5. Everything is restored - let objects re-sync with their linked objects.
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
    /// Removes map objects that no longer existed and restores states of the others.
    /// </summary>
    /// <param name="removed">Filled with map ids of the removed objects.</param>
    private void RestoreMapObjects(MapStateData data, HashSet<int> removed)
    {
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
    }

    /// <summary>
    /// Passes the stored player state to the <see cref="IPlayerStateProvider"/> in the simulation.
    /// </summary>
    private void RestorePlayerState(JToken playerState)
    {
        if (playerState is null || playerState.Type == JTokenType.Null)
            return;

        var provider = _simulation.Bodies.Select(body => body.Owner).OfType<IPlayerStateProvider>().FirstOrDefault();
        if (provider is null)
        {
            Console.WriteLine("No player state provider in the simulation - player state not restored.");
            return;
        }

        try
        {
            provider.RestorePlayerState(playerState.ToObject(provider.PlayerStateType, Serializer));
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to restore player state: {e}");
        }
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
