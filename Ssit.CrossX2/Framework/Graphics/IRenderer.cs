using Ssit.CrossX2.Framework.Graphics.Lighting;
using Ssit.CrossX2.Framework.Graphics.Renderers;

namespace Ssit.CrossX2.Framework.Graphics;

public interface IRenderer
{
    Size TargetSize { get; }
    RenderPass CurrentPass { get; }
    
    void Clear(RgbaColor black);
    
    ILightingManager LightingManager { get; }
    IStateManager StateManager { get; }
    IRenderStateProvider RenderStateProvider { get; }
    
    IGeometryRenderer GeometryRenderer { get; }
    ISpriteRenderer SpriteRenderer { get; }
    ITextRenderer TextRenderer { get; }
    IRenderQueue RenderQueue { get; }
}