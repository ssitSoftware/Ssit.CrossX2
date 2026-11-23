using System.Numerics;
using Ssit.CrossX2.Framework.Audio;
using Ssit.CrossX2.Framework.Games.Template;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Logic;

internal class Camera(IGameTemplate template, IInputMappings inputMappings): ICamera, ISoundListener
{
    private Vector2 _lookAt;

    private IPositionObject _primaryTarget;
    private Vector2 _primaryOffset;
    private float _primaryFollowFactor;
    
    private IPositionObject _temporaryTarget;
    private Vector2 _temporaryOffset;
    private float _temporaryReturnTime;
    private float _temporaryFollowFactor;
    
    private IPositionObject PositionObject => _temporaryTarget ?? _primaryTarget;
    private Vector2 BaseOffset => _temporaryTarget != null ? _temporaryOffset : _primaryOffset;
    private Vector2 StickOffset => _temporaryTarget != null ? Vector2.Zero : _cameraMove * 6;
    private float FollowFactor => _temporaryTarget != null ? _temporaryFollowFactor : _primaryFollowFactor;
    
    private Action _onTemporaryTargetFocused;

    public Vector2 LookAt => GetLookAt();

    private RectangleF? _visibleBounds;

    /// <summary>
    /// Set by the game instance on every render. Until the first frame is rendered it is approximated
    /// from the template target size centered at <see cref="LookAt"/>.
    /// </summary>
    public RectangleF VisibleBounds
    {
        get
        {
            if (_visibleBounds.HasValue)
                return _visibleBounds.Value;

            var size = new Vector2(template.TargetSize.Width, template.TargetSize.Height) / template.TileSize;
            var lookAt = LookAt;
            return new RectangleF(lookAt.X - size.X / 2, lookAt.Y - size.Y / 2, size.X, size.Y);
        }
        internal set => _visibleBounds = value;
    }

    private List<Vector2> _lastTargetPositions = new();
    private Vector2 _cameraMove;
    
    public Vector2 Position => LookAt;
    public float MaxDistance => VisibleArea * 2;
    public float PanDistance { get; } = (float)template.TargetSize.Width / template.TileSize / 2;
    public float VisibleArea { get; set; } = MathF.Max(template.TargetSize.Width, template.TargetSize.Height) / template.TileSize;

    private int? _cameraWindowWidth;
    private int? _cameraWindowHeight;

    private Vector2? _mapSize;
    private Vector2 _viewSize;

    /// <summary>
    /// Set by the game instance on every render. Lets the camera clamp its target to the map edges
    /// the same way <see cref="Rendering.Map.MapRenderer"/> does, before the right-stick offset is applied.
    /// </summary>
    internal void SetMapClampArea(Vector2 mapSize, Vector2 viewSize)
    {
        _mapSize = mapSize;
        _viewSize = viewSize;
    }

    private Vector2 ClampToMap(Vector2 lookAt)
    {
        if (!_mapSize.HasValue)
            return lookAt;

        var map = _mapSize.Value;
        var half = _viewSize / 2;

        // Mirrors MapRenderer.GetLayerRenderParameters (X: [half, mapW - half], Y: [half, mapH - half]).
        lookAt.X = MathF.Min(MathF.Max(half.X, lookAt.X), map.X - half.X);
        lookAt.Y = MathF.Min(MathF.Max(half.Y, lookAt.Y), map.Y - half.Y);
        return lookAt;
    }
    
    private Vector2 GetLookAt()
    {
        var lookAt = _lookAt;
        
        if (_cameraWindowWidth.HasValue)
        {
            var index = (int)(lookAt.X / _cameraWindowWidth.Value);
            lookAt.X = index * _cameraWindowWidth.Value + _cameraWindowWidth.Value / 2f;
        }
        
        if (_cameraWindowHeight.HasValue)
        {
            var index = (int)(lookAt.Y / _cameraWindowHeight.Value);
            lookAt.Y = index * _cameraWindowHeight.Value / 2f;
        }

        return lookAt;
    }
    
    public void SetPrimaryTarget(IPositionObject positionObject, Vector2 offset, float followFactor)
    {
        _primaryTarget = positionObject;
        _primaryOffset = offset;
        _primaryFollowFactor = followFactor;
        
        _lookAt = _primaryTarget.Position + _primaryOffset;
    }

    public void SnapToPrimaryTarget()
    {
        if (_primaryTarget is null)
            return;
        
        _lastTargetPositions.Clear();
        _lookAt = _primaryTarget.Position + _primaryOffset;
    }

    public void SetTemporaryTarget(IPositionObject positionObject, Vector2 offset, float followFactor, Action onFocused, TimeSpan returnAfter)
    {
        _temporaryTarget = positionObject;
        _temporaryOffset = offset;
        _temporaryReturnTime = (float)returnAfter.TotalSeconds;
        _onTemporaryTargetFocused = onFocused;
        _temporaryFollowFactor = followFactor;
    }

    public void RemoveTemporaryTarget()
    {
        _temporaryTarget = null;
        _temporaryOffset = Vector2.Zero;
        _temporaryReturnTime = 0f;
    }

    public void SetCameraWindow(int? width, int? height)
    {
        _cameraWindowWidth = width;
        _cameraWindowHeight = height;
    }
    
    public void Update(float dt)
    {
        if (PositionObject is null)
            return;

        var moveX = inputMappings[0].GetAxis("CameraX");
        var moveY = inputMappings[0].GetAxis("CameraY");

        var dir = new Vector2(moveX, moveY);
        _cameraMove = dir;

        // Clamp the followed position first, then add the stick offset and clamp again.
        // Otherwise, near a map edge the unclamped target lies far outside the visible range
        // and a stick offset pointing back into the map is swallowed by the render clamp.
        var target = ClampToMap(PositionObject.Position + BaseOffset);
        target = ClampToMap(target + StickOffset);
        _lastTargetPositions.Add(target);

        while (_lastTargetPositions.Count > 10)
        {
            _lastTargetPositions.RemoveAt(0);
        }

        var lastTarget = Vector2.Zero;
        foreach (var positions in _lastTargetPositions)
        {
            lastTarget += positions;
        }
        lastTarget /= _lastTargetPositions.Count;

        var targetDiff = target - lastTarget;
        var factorMul = 4 - targetDiff.Length();
        factorMul = MathF.Sqrt(MathF.Max(1, factorMul));

        if ((target - _lookAt).Length() <= 1)
        {
            factorMul *= factorMul;
        }
        factorMul /= 2;
        factorMul = MathF.Max(1, factorMul);
        
        if (targetDiff != Vector2.Zero)
        {
            factorMul = 1;
        }
        
        if (_primaryFollowFactor == 0)
        {
            _lookAt = target;
            return;
        }
        
        var factor = MathF.Min(1, FollowFactor * dt * factorMul);
        var newLookAt = factor * target + (1 - factor) * _lookAt;
        
        var diff = newLookAt - target;
        
        var epsilon = 0.1f / template.TileSize;

        if (MathF.Abs(diff.X) < epsilon && MathF.Abs(diff.Y) < epsilon)
        {
            newLookAt = target;
        }

        _lookAt = newLookAt;
        
        if (_temporaryTarget != null && (_lookAt - target).Length() < 0.5f)
        {
            _onTemporaryTargetFocused?.Invoke();
            _onTemporaryTargetFocused = null;
            
            _temporaryReturnTime -= dt;
            if (_temporaryReturnTime <= 0)
            {
                _temporaryTarget = null;
                _temporaryOffset = Vector2.Zero;
                _temporaryReturnTime = 0f;
            }
        }
    }
}