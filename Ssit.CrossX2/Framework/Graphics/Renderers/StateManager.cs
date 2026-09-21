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
        _state = new RenderState(1, Vector2.Zero, BlendMode.AlphaBlend, TextureFilter.Point, null);
        UpdateHwMode();
    }

    public void Scale(float scale)
    {
        if (Math.Abs(scale - 1) < float.Epsilon) return;
        
        _state = new RenderState(_state.Scale * scale, _state.Offset, _state.BlendMode, _state.TextureFilter, _state.ClipRect);
        UpdateHwMode();
    }

    public void Translate(Vector2 offset)
    {
        if(offset == Vector2.Zero) return;
        
        _state = new RenderState(_state.Scale, _state.Offset + offset * _state.Scale, _state.BlendMode, _state.TextureFilter, _state.ClipRect);
        UpdateHwMode();
    }
    
    public void SetBlendMode(BlendMode blendMode)
    {
        if(_state.BlendMode == blendMode) return;
        
        _state = new RenderState(_state.Scale, _state.Offset, blendMode, _state.TextureFilter, _state.ClipRect);
        UpdateHwMode();
    }
    
    public void SetClipRect(RectangleF? clipRect, bool intersectExisting = true)
    {
        if (!clipRect.HasValue && _state.ClipRect == null) return;
        if (clipRect.HasValue && _state.ClipRect.HasValue && _state.ClipRect.Value.Equals(clipRect.Value)) return;

        if (clipRect.HasValue)
        {
            var scale = _state.Scale;
            var offset = _state.Offset;
            
            var r = clipRect.Value;
            
            var x =  r.X * scale + offset.X;
            var y =  r.Y * scale + offset.Y;
            
            var w = r.Width * scale;
            var h = r.Height * scale;
            
            clipRect = new RectangleF(x, y, w, h);

            if (intersectExisting && _state.ClipRect.HasValue)
            {
                clipRect = clipRect.Value.Intersect(_state.ClipRect.Value);
            }
        }
        
        _state = new RenderState(_state.Scale, _state.Offset, _state.BlendMode, _state.TextureFilter, clipRect);
        UpdateHwMode();
    }

    public void SetTextureFilter(TextureFilter filter)
    {
        if(_state.TextureFilter == filter) return;
        
        _state = new RenderState(_state.Scale, _state.Offset, _state.BlendMode, filter, _state.ClipRect);
        UpdateHwMode();
    }

    private void UpdateHwMode()
    {
        _handler?.HwModeUpdated();
        _stateProvider.Update(_state);
    }

    private RenderState _state = new(1, Vector2.Zero, BlendMode.AlphaBlend, TextureFilter.Point, null);
    private RenderStateProvider _stateProvider = new();
    private readonly IUpdateHwModeHandler _handler;

    public StateManager(IUpdateHwModeHandler handler)
    {
        _handler = handler;
        _stateProvider.Update(_state);
    }
}
