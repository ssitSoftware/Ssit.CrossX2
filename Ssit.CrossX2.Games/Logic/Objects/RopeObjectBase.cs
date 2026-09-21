using System.Numerics;
using Ssit.CrossX2.Framework.Games.Editor;
using Ssit.CrossX2.Framework.Games.Physics;
using Ssit.CrossX2.Framework.Games.Physics.Coliders;
using Ssit.CrossX2.Framework.Games.Platformer.Builders;
using Ssit.CrossX2.Framework.Games.Rendering;
using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public abstract class RopeObjectBase : IBodyOwner, IGameObjectRenderer, IPendulum
{
    public class Parameters
    {
        [EditorLink(typeof(ITarget))] public int End { get; set; }
        [EditorFloat(0, 10, 0.1f)] public float WindStrength { get; set; } = 1f;
    }
    
    private const float PendulumDamping = 0.995f;
    private const float RopeDamping = 0.98f;
    private const int ConstraintIterations = 5;
    private const float WindPrimaryAmplitude = 0.5f;
    private const float WindSecondaryAmplitude = 0.25f;
    private const float MaxSwingAngle = MathF.PI * 80f / 180f;
    private const float SwingBrakeStartAngle = MathF.PI * 60f / 180f;
    private const float SwingBrakeStrength = 12f;

    private readonly float _windStrength;
    private readonly Vector2 _anchorPosition;
    private readonly IMaterial _ropeTriggerMaterial;

    private ITarget _endTarget;
    private int _segmentCount;
    private Vector2[] _positions;
    private Vector2[] _prevPositions;
    private float _segmentLength;
    private IPendulumSwinger _swinger;
    private int _attachSegment = -1;
    private float _pendulumAngle;
    private float _pendulumAngularVelocity;
    private float _windTime;
    private ICollider _triggerCollider;

    private readonly float _maxSegmentDistance;
    
    protected GameObjectsServices Services { get; }

    public int ZOrder { get; }
    public virtual RectangleF Bounds { get; protected set; }
    public IBody Body { get; }
    
    private readonly bool _isBackground;

    protected RopeObjectBase(GameObjectsServices services, ObjectCreationParameters<Parameters> parameters, float segmentDistance, IMaterial ropeTriggerMaterial, bool isBackground = false)
    {
        Services = services;

        _maxSegmentDistance = segmentDistance;
        _anchorPosition = parameters.Position;
        _windStrength = parameters.Parameters.WindStrength;
        _isBackground = isBackground;
        
        ZOrder = parameters.ZOrder;
        _ropeTriggerMaterial = ropeTriggerMaterial;

        _windTime = Random.Shared.NextSingle() * 17f;
        
        Body = services.Simulation.CreateBody(this);
        Body.IsKinematic = true;
        Body.Position = parameters.Position;
        Bounds = new RectangleF(parameters.Position - new Vector2(30, 30), new SizeF(60, 60));
        
        parameters.LinkMap.RequestLink<ITarget>(parameters.Parameters.End, SetEndTarget);
    }

    private void SetEndTarget(ITarget target)
    {
        _endTarget = target;
        InitializeRope();
    }

    private void InitializeRope()
    {
        var start = _anchorPosition;
        var end = _endTarget.Position;
        var totalLength = Vector2.Distance(start, end);
        _segmentCount = Math.Max(2, (int)MathF.Ceiling(totalLength / _maxSegmentDistance) + 1);
        _segmentLength = totalLength / (_segmentCount - 1);

        _positions = new Vector2[_segmentCount];
        _prevPositions = new Vector2[_segmentCount];

        for (var i = 0; i < _segmentCount; i++)
        {
            var t = (float)i / (_segmentCount - 1);
            _positions[i] = Vector2.Lerp(start, end, t);
            _prevPositions[i] = _positions[i];
        }

        var pad = new Vector2(totalLength + 1, totalLength + 1);
        Bounds = new RectangleF(start - pad with { Y = 1 }, new SizeF(pad.X * 2, pad.Y + 1));

        if (_triggerCollider == null && !_isBackground)
        {
            _triggerCollider = Services.Simulation.CreateCollider(new RectColliderCreationParameters
            {
                Center = new Vector2(0, totalLength / 2),
                Size = new SizeF(totalLength * 2, totalLength),
                Type = ColliderType.Trigger,
                Active = true,
                AttachToBody = Body,
                Material = _ropeTriggerMaterial
            });
            Body.AddColliders(_triggerCollider);
        }
    }

    public void AppendVelocity(float velocity)
    {
        if (_attachSegment <= 0) return;
        var armLength = _attachSegment * _segmentLength;
        if (armLength > 0.01f)
            _pendulumAngularVelocity += velocity / armLength;
    }

    public bool CanAttach(Aabb swingerAabb)
    {
        foreach (var segPos in _positions)
        {
            if (swingerAabb.Contains(segPos))
                return true;
        }
        return false;
    }

    public void AttachObject(IPendulumSwinger swinger)
    {
        _swinger = swinger;
        _attachSegment = FindClosestSegment(swinger.Position);

        if (_positions != null && _attachSegment > 0)
        {
            var delta = _positions[_attachSegment] - _anchorPosition;
            _pendulumAngle = MathF.Atan2(delta.X, delta.Y);
            _pendulumAngularVelocity = 0;
        }

        if (_positions != null && _attachSegment >= 0)
            swinger.OnAttachPosition(_positions[_attachSegment]);
    }

    public void DetachObject(IPendulumSwinger swinger)
    {
        if (_swinger == swinger)
        {
            _swinger = null;
            _attachSegment = -1;
        }
    }
    
    Vector2 IPendulum.AnchorPosition=> _positions?[0] ?? Vector2.Zero;

    private int FindClosestSegment(Vector2 position)
    {
        if (_positions == null) return 0;

        var closest = 0;
        var minDist = float.MaxValue;
        for (var i = 0; i < _positions.Length; i++)
        {
            var dist = Vector2.Distance(_positions[i], position);
            if (dist < minDist) { minDist = dist; closest = i; }
        }
        return closest;
    }

    public Aabb GetBoundingBox()
    {
        var minX = float.MaxValue;
        var minY = float.MaxValue;
        var maxX = float.MinValue;
        var maxY = float.MinValue;
        
        foreach (var pos in _positions)
        {
            minX = Math.Min(minX, pos.X);
            minY = Math.Min(minY, pos.Y);
            maxX = Math.Max(maxX, pos.X);
            maxY = Math.Max(maxY, pos.Y);
        }
        
        return new Aabb(minX, minY, maxX, maxY);
    }
    
    public void OnFixedUpdate(out bool cancelUpdate)
    {
        cancelUpdate = false;

        if (_positions == null)
        {
            if (_endTarget == null) return;
            InitializeRope();
        }

        var dt = Services.Simulation.SimulationParameters.TimeDelta;
        var gravity = Services.GameTemplate.Gravity;

        if (_swinger != null && _attachSegment > 0)
        {
            UpdatePendulumPortion(dt, gravity);
            UpdateVerletPortion(dt, gravity);
        }
        else
        {
            UpdateFullVerlet(dt, gravity);
        }

        if (_swinger != null && _attachSegment >= 0)
            _swinger.OnAttachPosition(_positions![_attachSegment]);
    }

    private void UpdateFullVerlet(float dt, Vector2 gravity)
    {
        _windTime += dt;
        var windX = (MathF.Sin(_windTime * 3.1f) * WindPrimaryAmplitude
                   + MathF.Sin(_windTime * 7.3f) * WindSecondaryAmplitude) * _windStrength * dt * dt;

        for (var i = 1; i < _segmentCount; i++)
        {
            var vel = (_positions[i] - _prevPositions[i]) * RopeDamping;
            _prevPositions[i] = _positions[i];
            var weight = (float)i / (_segmentCount - 1);
            _positions[i] += vel + gravity * (dt * dt) + new Vector2(windX * weight, 0);
        }

        PinAnchor();
        for (var iter = 0; iter < ConstraintIterations; iter++)
            SolveConstraints(0);
    }

    private void UpdatePendulumPortion(float dt, Vector2 gravity)
    {
        var anchor = _anchorPosition;
        var armLength = _attachSegment * _segmentLength;

        if (armLength < 0.01f) return;

        var angAccel = -(gravity.Y / armLength) * MathF.Sin(_pendulumAngle);
        _pendulumAngularVelocity += angAccel * dt;
        _pendulumAngularVelocity *= PendulumDamping;

        // Progressive braking as the swing approaches the max angle
        var absAngle = MathF.Abs(_pendulumAngle);
        if (absAngle > SwingBrakeStartAngle)
        {
            var brakeT = (absAngle - SwingBrakeStartAngle) / (MaxSwingAngle - SwingBrakeStartAngle);
            brakeT = Math.Clamp(brakeT, 0f, 1f);
            _pendulumAngularVelocity *= MathF.Exp(-brakeT * brakeT * SwingBrakeStrength * dt);
        }

        _pendulumAngle += _pendulumAngularVelocity * dt;

        // Hard clamp at max angle
        if (MathF.Abs(_pendulumAngle) > MaxSwingAngle)
        {
            _pendulumAngle = MathF.Sign(_pendulumAngle) * MaxSwingAngle;
            _pendulumAngularVelocity = 0f;
        }

        for (var i = 0; i <= _attachSegment; i++)
        {
            var r = (float)i / _attachSegment * armLength;
            _positions[i] = anchor + new Vector2(MathF.Sin(_pendulumAngle) * r, MathF.Cos(_pendulumAngle) * r);
            _prevPositions[i] = _positions[i];
        }
    }

    private void UpdateVerletPortion(float dt, Vector2 gravity)
    {
        for (var i = _attachSegment + 1; i < _segmentCount; i++)
        {
            var vel = (_positions[i] - _prevPositions[i]) * RopeDamping;
            _prevPositions[i] = _positions[i];
            _positions[i] += vel + gravity * (dt * dt);
        }

        _prevPositions[_attachSegment] = _positions[_attachSegment];
        for (var iter = 0; iter < ConstraintIterations; iter++)
            SolveConstraints(_attachSegment);
    }

    private void PinAnchor()
    {
        _positions[0] = _anchorPosition;
        _prevPositions[0] = _anchorPosition;
    }

    private void SolveConstraints(int pinIndex)
    {
        _positions[pinIndex] = _prevPositions[pinIndex];

        for (var i = pinIndex; i < _segmentCount - 1; i++)
        {
            var delta = _positions[i + 1] - _positions[i];
            var dist = delta.Length();
            if (dist < 0.0001f) continue;

            var correction = delta / dist * (dist - _segmentLength) * 0.5f;

            if (i == pinIndex)
                _positions[i + 1] -= correction * 2f;
            else
            {
                _positions[i] += correction;
                _positions[i + 1] -= correction;
            }
        }
    }

    public void Render(IRenderer renderer, RgbaColor color)
    {
        if (_positions == null) return;

        var tileSize = Services.GameTemplate.TileSize;
        
        for (var i = 0; i < _positions.Length; i++)
        {
            var screenPos = _positions[i] * tileSize;

            var dir = new Vector2(0, -1);
            if (i > 0)
            {
                dir = Vector2.Normalize(_positions[i] - _positions[i - 1]);
            }

            if (i < _positions.Length - 1)
            {
                var dir2 = Vector2.Normalize(_positions[i + 1] - _positions[i]);
                dir = Vector2.Lerp(dir, dir2, 0.5f);
            }
            
            RenderSegment(renderer, i,  _positions.Length, screenPos, dir);
        }
    }

    protected abstract void RenderSegment(IRenderer renderer, int segmentIndex, int segmentCount, Vector2 screenPosition, Vector2 direction);

    void IDisposable.Dispose() => OnDispose();

    public virtual void OnDispose()
    {
    }
}
