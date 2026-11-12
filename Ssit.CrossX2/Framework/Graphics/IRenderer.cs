using Ssit.CrossX2.Framework.Graphics.Lighting;
using Ssit.CrossX2.Framework.Graphics.Renderers;

namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// Main rendering entry point: owns the render state and gives access to the specialized renderers.
/// </summary>
public interface IRenderer
{
    /// <summary>
    /// Size in pixels of the current render target (or the window when none is set).
    /// </summary>
    Size TargetSize { get; }
    /// <summary>
    /// Pass being rendered (normal color or glow).
    /// </summary>
    RenderPass CurrentPass { get; }
    
    /// <summary>
    /// Clears the current render target to the given color.
    /// </summary>
    void Clear(RgbaColor black);
    
    /// <summary>
    /// Ambient and dynamic lights.
    /// </summary>
    ILightingManager LightingManager { get; }
    /// <summary>
    /// Changes the render state (transform, blending, clipping, tint).
    /// </summary>
    IStateManager StateManager { get; }
    /// <summary>
    /// Reads the current render state.
    /// </summary>
    IRenderStateProvider RenderStateProvider { get; }
    
    /// <summary>
    /// Draws shapes and primitives.
    /// </summary>
    IGeometryRenderer GeometryRenderer { get; }
    /// <summary>
    /// Draws textures and sprites.
    /// </summary>
    ISpriteRenderer SpriteRenderer { get; }
    /// <summary>
    /// Draws text with a given font.
    /// </summary>
    ITextRenderer TextRenderer { get; }
    /// <summary>
    /// Queue for deferred, ordered drawing.
    /// </summary>
    IRenderQueue RenderQueue { get; }
    /// <summary>
    /// Redirects drawing into renderTarget; null draws to the window.
    /// </summary>
    void SetRenderTarget(IRenderTarget renderTarget);
}