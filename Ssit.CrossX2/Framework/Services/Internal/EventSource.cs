namespace Ssit.CrossX2.Framework.Services.Internal;

internal class EventSource: IEventSource
{
    public event Action<float> Updating;
    public event Action Updated;
    public event Action RenderFinished;
    public event Action Paused;
    public event Action Resumed;

    public void OnUpdate( float deltaTime ) => Updating?.Invoke(deltaTime);

    public void OnUpdated() => Updated?.Invoke();

    public void OnRenderFinished() => RenderFinished?.Invoke();

    public void OnPause() => Paused?.Invoke();
    public void OnResume() => Resumed?.Invoke();
}