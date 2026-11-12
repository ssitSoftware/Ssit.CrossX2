namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// Pass currently being rendered.
/// </summary>
public enum RenderPass
{
    /// <summary>
    /// Regular color pass.
    /// </summary>
    Normal,
    /// <summary>
    /// Glow pass - only glowing parts are drawn into the glow target.
    /// </summary>
    Glow
}