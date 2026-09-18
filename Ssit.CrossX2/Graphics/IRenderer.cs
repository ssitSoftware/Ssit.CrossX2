using Ssit.CrossX2.Graphics.Lighting;
using Ssit.CrossX2.Graphics.Renderers;

namespace Ssit.CrossX2.Graphics;

public interface IRenderer
{
    Size TargetSize { get; }
    RenderPass CurrentPass { get; }
    
    void Clear(RgbaColor black);
    
    ILightingManager LightingManager { get; }
    IStateManager StateManager { get; }
    IRenderStateProvider RenderStateProvider { get; }
    
    IPrimitiveRenderer PrimitiveRenderer { get; }
    IGeometryRenderer GeometryRenderer { get; }
    ISpriteRenderer SpriteRenderer { get; }
    ITextRenderer TextRenderer { get; }
}