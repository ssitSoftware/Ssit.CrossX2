using System.Numerics;
using Ssit.CrossX2.Core;
using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.UI.Services;
using Ssit.CrossX2.UI.Views;

namespace Ssit.CrossX2.UI;

internal class UiAppComponent(IAppHost host, IRenderer renderer, UiAppComponent.Parameters parameters) : IAppComponent
{
    public class Parameters
    {
        public IUiAppInternal UiApp { get; init; }
        public ColorWrapper BackgroundColor { get; init; }
        
    }
    
    private readonly IUiAppInternal _app = parameters.UiApp;
    private readonly ColorWrapper _backgroundColor = parameters.BackgroundColor;
    
    void IDisposable.Dispose()
    {
        OnDispose();
        GC.SuppressFinalize(this);
    }

    void IAppComponent.SetActive(bool active) => IsActive = active;
    void IAppComponent.Update( float dt ) => OnUpdate(dt);
    void IAppComponent.Draw() => OnDraw();
    void IAppComponent.Resize() => OnResize();

    public bool IsActive { get; private set; }

    private void OnDispose() => _app.Dispose();

    private void OnResize() => _app.SetBounds(new RectangleF(Vector2.Zero, host.TargetSize / host.Scale), host.Scale);

    protected virtual void OnDraw()
    {
        if (!IsActive)
            return;
        
        _app.Draw(renderer, _backgroundColor.GetColor(renderer) ?? RgbaColor.Black);
    }

    private void OnUpdate(float elapsedTime) => _app.Update(elapsedTime);
}