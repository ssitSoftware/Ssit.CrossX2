using System.Numerics;

namespace Ssit.CrossX2.Framework.Graphics.Renderers;

public class StateManager : IStateManager
{
    public interface IUpdateHwModeHandler
    {
        void HwModeUpdated();
    }

    public IRenderStateProvider StateProvider => _stateProvider;
    
    private readonly Stack<RenderState> _savedStates = new ();
    
    public void SaveState()
    {
        _savedStates.Push(_state);
    }

    public void RestoreState()
    {
        var blendMode = _state.BlendMode;
        var clipRect = _state.ClipRect;
        
        _state = _savedStates.Pop();
        
        if (blendMode != _state.BlendMode || clipRect != _state.ClipRect)
        {
            UpdateHwMode();
        }
    }

    public void Reset()
    {
        _savedStates.Clear();
        _state = new();
        UpdateHwMode();
    }

    public void Scale(float scale)
    {
        if (Math.Abs(scale - 1) < float.Epsilon) return;

        _state.Transform = Matrix4x4.CreateScale(scale) * _state.Transform;
        UpdateHwMode();
    }

    public void Translate(Vector2 offset)
    {
        if(offset == Vector2.Zero) return;
        
        _state.Transform = Matrix4x4.CreateTranslation(new Vector3(offset, 0)) * _state.Transform;
        UpdateHwMode();
    }
    
    public void SetBlendMode(BlendMode blendMode)
    {
        if(_state.BlendMode == blendMode) return;
        
        _state.BlendMode = blendMode;
        UpdateHwMode();
    }
    
    public void SetClipRect(RectangleF? clipRect, bool intersectExisting = true)
    {
        if (!clipRect.HasValue && _state.ClipRect == null) return;
        if (clipRect.HasValue && _state.ClipRect.HasValue && _state.ClipRect.Value.Equals(clipRect.Value)) return;

        if (clipRect.HasValue)
        {
            var r = clipRect.Value;
            
            var tl = Vector3.Transform(new Vector3(r.TopLeft, 0), _state.Transform);
            var br = Vector3.Transform(new Vector3(r.BottomRight, 0), _state.Transform);
            var tr = Vector3.Transform(new Vector3(r.TopRight, 0), _state.Transform);
            var bl = Vector3.Transform(new Vector3(r.BottomLeft, 0), _state.Transform);
            
            var minX = MathF.Min(MathF.Min(tl.X, tr.X), MathF.Min(bl.X, br.X));
            var minY = MathF.Min(MathF.Min(tl.Y, tr.Y), MathF.Min(bl.Y, br.Y));
            
            var maxX = MathF.Max(MathF.Max(tl.X, tr.X), MathF.Max(bl.X, br.X));
            var maxY = MathF.Max(MathF.Max(tl.Y, tr.Y), MathF.Max(bl.Y, br.Y));
            
            clipRect = new RectangleF(minX, minY, maxX - minX, maxY - minY);

            if (intersectExisting && _state.ClipRect.HasValue)
            {
                clipRect = clipRect.Value.Intersect(_state.ClipRect.Value);
            }
        }
        
        _state.ClipRect = clipRect;
        UpdateHwMode();
    }

    public void SetTextureFilter(TextureFilter filter)
    {
        if(_state.TextureFilter == filter) return;
        
        _state.TextureFilter = filter;
        UpdateHwMode();
    }

    private void UpdateHwMode()
    {
        _handler?.HwModeUpdated();
        _stateProvider.Update(_state);
    }

    private RenderState _state = new();
    private RenderStateProvider _stateProvider = new();
    private readonly IUpdateHwModeHandler _handler;

    public StateManager(IUpdateHwModeHandler handler)
    {
        _handler = handler;
        _stateProvider.Update(_state);
    }
}
