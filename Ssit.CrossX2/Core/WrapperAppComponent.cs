namespace Ssit.CrossX2.Core;

public abstract class WrapperAppComponent(IAppComponent component) : IAppComponent
{
    void IDisposable.Dispose() => OnDispose();
    void IAppComponent.SetActive(bool active) => OnSetActive(active);
    void IAppComponent.Update(float dt) => OnUpdate(dt);
    void IAppComponent.Draw() => OnDraw();
    void IAppComponent.Resize() => OnResize();
    
    protected virtual void OnDispose() => component.Dispose();
    protected virtual void OnSetActive(bool active) => component.SetActive(active);
    protected virtual void OnUpdate(float dt) => component.Update(dt);
    protected virtual void OnDraw() => component.Draw();
    protected virtual void OnResize() => component.Resize();
}