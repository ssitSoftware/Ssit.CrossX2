using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.IoC;

namespace Ssit.CrossX2.UI.Services;

public interface IUiApp: IDisposable
{
    public IIoCContainer Services { get; }
    public INavigation Navigation { get; }
    void LoadStyles(params Type[] types);
}

internal interface IUiAppInternal: IUiApp
{
    void Update(float dt);
    void Draw(IRenderer renderer, RgbaColor? clearColor = null);
    void SetBounds(RectangleF bounds, float scale);
}