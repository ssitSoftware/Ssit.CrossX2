using System.Numerics;
using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Platformer.BodyExtensions;

public class SlideFromEdgeExtension: IBodyExtension, IBodyEventsReceiver
{
    private readonly IBody _body;
    private readonly List<ICollider> _collidersBuffer = new();

    private readonly float _slideSpeed = 0;
    private readonly HashSet<int> _noSlideMaterialIndices;
    private readonly float _stableWidthNormalized = 0;

    void IBodyEventsReceiver.OnBodyUpdated() => OnBodyUpdated();
    void IBodyEventsReceiver.OnBodyDisposed() => BodyExtensions.List.Remove(this);
    
    public static void Attach(IBody body, float stableWidthNormalized, float slideSpeed, params int[] noSlideMaterialIndices)
    {
        BodyExtensions.List.Add(new SlideFromEdgeExtension(body, stableWidthNormalized, slideSpeed, noSlideMaterialIndices));
    }
    
    private SlideFromEdgeExtension(IBody body, float stableWidthNormalized, float slideSpeed,
        int[] noSlideMaterialIndices)
    {
        _body = body;
        _body.AddEventsReceiver(this);
        
        _slideSpeed = slideSpeed;
        _noSlideMaterialIndices = new (noSlideMaterialIndices);
        _stableWidthNormalized = stableWidthNormalized;
    }

    private void OnBodyUpdated()
    {
        foreach (var collider in _body.Colliders)
        {
            var aabb = collider.Aabb;
            _collidersBuffer.Clear();

            var group = collider.Material.ColliderGroup;
            
            if (!_body.Simulation.CheckCollision(new Aabb(aabb.Left, aabb.Bottom - 0.02f, aabb.Right, aabb.Bottom + 0.05f), _body, group, 0, _collidersBuffer)) return;

            for (var idx = 0; idx < _collidersBuffer.Count; )
            {
                if(!_collidersBuffer[idx].Material.Sides.HasFlag(ColliderSides.Top))
                {
                    _collidersBuffer.RemoveAt(idx);
                    continue;
                }

                if (_collidersBuffer[idx].Aabb.Top < aabb.Bottom)
                {
                    _collidersBuffer.RemoveAt(idx);
                    continue;
                }

                idx++;
            }

            if (_collidersBuffer.Count == 0) return;

            var slideLeft = aabb.Width / 2;
            var slideRight = aabb.Width / 2;

            for (var idx = 0; idx < _collidersBuffer.Count; ++idx)
            {
                if (_noSlideMaterialIndices.Contains(_collidersBuffer[idx].Material.Index))
                {
                    slideLeft = 0;
                    slideRight = 0;
                    break;
                }
                
                var colliderAabb = _collidersBuffer[idx].Aabb;
                slideLeft = MathF.Min(slideLeft, MathF.Max(0, colliderAabb.Left - aabb.Left - aabb.Width * _stableWidthNormalized));
                slideRight = MathF.Min(slideRight, MathF.Max(0, aabb.Right - aabb.Width * _stableWidthNormalized - colliderAabb.Right));
            }

            if (slideLeft > 0)
            {
                slideLeft /= aabb.Width * _stableWidthNormalized;
                _body.Velocity -= new Vector2(_body.Simulation.SimulationParameters.TimeDelta * _slideSpeed * MathF.Pow(slideLeft, 0.25f), 0);
                _body.Touch();
            }
            else if (slideRight > 0)
            {
                slideRight /= aabb.Width * _stableWidthNormalized;
                _body.Velocity += new Vector2(_body.Simulation.SimulationParameters.TimeDelta * _slideSpeed * MathF.Pow(slideRight, 0.25f), 0);
                _body.Touch();
            }
        }
    }
}