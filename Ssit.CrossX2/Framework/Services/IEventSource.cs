namespace Ssit.CrossX2.Framework.Services;

/// <summary>
/// Publishes application lifecycle, frame update and fullscreen transition events.
/// </summary>
public interface IEventSource
{
    event Action<float> Updating;
    event Action Updated;
    event Action RenderFinished;

    event Action Paused;
    event Action Resumed;
    event Action EnteredFullscreen;
    event Action LeftFullscreen;
}
