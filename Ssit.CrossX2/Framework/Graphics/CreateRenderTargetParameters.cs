namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// Parameters for constructing an <see cref="IRenderTarget"/> through the IoC container.
/// </summary>
public class CreateRenderTargetParameters
{
    /// <summary>
    /// Size of the render target in pixels.
    /// </summary>
    public Size Size { get; set; }
}