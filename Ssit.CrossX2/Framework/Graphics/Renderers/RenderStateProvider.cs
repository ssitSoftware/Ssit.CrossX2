using System.Numerics;

namespace Ssit.CrossX2.Framework.Graphics.Renderers;

internal class RenderStateProvider : IRenderStateProvider
{
    private RenderState _state;
    
    public void Update(RenderState state) => _state = state;
    
    float IRenderStateProvider.Scale => _state.Scale;
    Vector2 IRenderStateProvider.Offset => _state.Offset;
    public BlendMode BlendMode => _state.BlendMode;
    public TextureFilter TextureFilter => _state.TextureFilter;
    public RectangleF? ClipRect => _state.ClipRect;
}