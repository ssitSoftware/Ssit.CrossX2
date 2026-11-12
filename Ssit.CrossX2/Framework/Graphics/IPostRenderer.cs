namespace Ssit.CrossX2.Framework.Graphics;

/// <summary>
/// Custom drawing performed directly on the screen after the render host has rendered the game frame
/// (e.g. overlays rendered at native resolution).
/// </summary>
public interface IPostRenderer: IDisposable
{
    /// <summary>
    /// Called at the end of the frame. targetViewport is the screen area the game image was drawn into.
    /// </summary>
    void Render(RectangleF targetViewport);
}