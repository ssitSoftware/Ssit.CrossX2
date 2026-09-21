using System.Numerics;
using Ssit.CrossX2.Framework.Games.Template;
using Ssit.CrossX2.Framework.Input;

namespace Ssit.CrossX2.Framework.Games.Logic;

internal class Camera(IGameTemplate template, IInputMappings inputMappings): ICamera
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
    private Vector2 Offset => _temporaryTarget != null ? _temporaryOffset : _primaryOffset + _cameraMove * 6;
    private float FollowFactor => _temporaryTarget != null ? _temporaryFollowFactor : _primaryFollowFactor;
    
    private Action _onTemporaryTargetFocused;

    public Vector2 LookAt => GetLookAt();

    private List<Vector2> _lastTargetPositions = new();
    private Vector2 _cameraMove;
    
    private int? _cameraWindowWidth;
    private int? _cameraWindowHeight;
    
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

        var target = PositionObject.Position + Offset;
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