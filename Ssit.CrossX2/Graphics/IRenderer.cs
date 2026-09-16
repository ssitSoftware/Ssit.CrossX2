using CrossX2.Graphics.Renderers;

namespace CrossX2.Graphics;

public interface IRenderer
{
    Size TargetSize { get; }
    RenderPass CurrentPass { get; }
    
    IStateManager StateManager { get; }
    IRenderStateProvider RenderStateProvider { get; }
    
    IPrimitiveRenderer PrimitiveRenderer { get; }
    IGeometryRenderer GeometryRenderer { get; }
}