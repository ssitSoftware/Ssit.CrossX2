using System.Numerics;

namespace Ssit.CrossX2.Framework.Graphics.Renderers;

internal class RenderStateProvider : IRenderStateProvider
{
    private RenderState _state;
    
    public Matrix4x4 Transform => _state.Transform;
    public BlendMode BlendMode => _state.BlendMode;
    public TextureFilter TextureFilter => _state.TextureFilter;
    public RectangleF? ClipRect => _state.ClipRect;

    public float Scale { get; private set; }

    public void Update(RenderState state)
    {
        _state = state;
        
        var vec = Vector3.TransformNormal(Vector3.Normalize(new Vector3(new Vector2(1, 1), 0)), _state.Transform);
        Scale = vec.Length();
    }
}