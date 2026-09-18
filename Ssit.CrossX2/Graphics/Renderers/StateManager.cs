using System.Numerics;

namespace Ssit.CrossX2.Graphics.Renderers;

public class StateManager(StateManager.IUpdateHwModeHandler handler) : IStateManager, IRenderStateProvider
{
    public interface IUpdateHwModeHandler
    {
        void UpdateHwMode(BlendMode blendMode, RectangleF? clipRect, IRenderTarget renderTarget);
    }

    private readonly struct State(float scale, Vector2 offset, BlendMode blendMode, TextureFilter textureFilter, RectangleF? clipRect, IRenderTarget renderTarget)
    {
        public readonly float Scale = scale;
        public readonly Vector2 Offset = offset;
        public readonly BlendMode BlendMode = blendMode;
        public readonly TextureFilter TextureFilter = textureFilter;
        public readonly RectangleF? ClipRect = clipRect; 
        public readonly IRenderTarget RenderTarget = renderTarget;
    }
    
    private readonly Stack<State> _savedStates = new ();
    
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
        _state = new State(1, Vector2.Zero, BlendMode.AlphaBlend, TextureFilter.Point, null, null);
        UpdateHwMode();
    }

    public void Scale(float scale)
    {
        _state = new State(_state.Scale * scale, _state.Offset, _state.BlendMode, _state.TextureFilter, _state.ClipRect, _state.RenderTarget);
    }

    public void Translate(Vector2 offset)
    {
        _state = new State(_state.Scale, _state.Offset + offset * _state.Scale, _state.BlendMode, _state.TextureFilter, _state.ClipRect, _state.RenderTarget);
    }
    
    public void SetBlendMode(BlendMode blendMode)
    {
        if(_state.BlendMode == blendMode) return;
        
        _state = new State(_state.Scale, _state.Offset, blendMode, _state.TextureFilter, _state.ClipRect, _state.RenderTarget);
        UpdateHwMode();
    }

    public void SetRenderTarget(IRenderTarget renderTarget)
    {
        _state = new State(_state.Scale, _state.Offset, _state.BlendMode, _state.TextureFilter, _state.ClipRect, renderTarget);
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
        
        _state = new State(_state.Scale, _state.Offset, _state.BlendMode, _state.TextureFilter, clipRect, _state.RenderTarget);
        UpdateHwMode();
    }

    public void SetTextureFilter(TextureFilter filter)
    {
        _state = new State(_state.Scale, _state.Offset, _state.BlendMode, filter, _state.ClipRect, _state.RenderTarget);
    }

    private void UpdateHwMode()
    {
        handler?.UpdateHwMode(_state.BlendMode, _state.ClipRect, _state.RenderTarget);
    }
    
    private State _state = new(1, Vector2.Zero, BlendMode.AlphaBlend, TextureFilter.Point, null, null);

    float IRenderStateProvider.Scale => _state.Scale;
    Vector2 IRenderStateProvider.Offset => _state.Offset;
    public BlendMode BlendMode => _state.BlendMode;
    public TextureFilter TextureFilter => _state.TextureFilter;
    public RectangleF? ClipRect => _state.ClipRect;
}
