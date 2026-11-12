namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// Configuration of the <see cref="Ssit.CrossX2.Framework.Core.IRenderHost"/>: the game renders into an offscreen target derived from the design size,
/// which is then scaled to the window.
/// </summary>
public interface IRenderHostParameters
{
    /// <summary>
    /// Logical resolution the game is designed for.
    /// </summary>
    Size DesignSize { get; set; }
    /// <summary>
    /// Scaling mode and optional effects.
    /// </summary>
    RenderHostFlags Flags { get; set; }
    /// <summary>
    /// Smallest allowed integer scale of the design size.
    /// </summary>
    int MinScale { get; set; }
    /// <summary>
    /// Largest allowed integer scale of the design size.
    /// </summary>
    int MaxScale { get; set; }
    /// <summary>
    /// Optional drawing done on the screen after the frame is presented.
    /// </summary>
    IPostRenderer PostRenderer { get; set; }
}