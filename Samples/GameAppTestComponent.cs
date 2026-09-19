using Ssit.CrossX2;
using Ssit.CrossX2.Content;
using Ssit.CrossX2.Core;
using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.Graphics.Renderers;
using Ssit.CrossX2.Services;

namespace Samples;

public class GameAppTestComponent(IRenderer renderer, IContentManager contentManager, IAppTimer timer) : IAppComponent
{
    private ResourceHandle<ITexture> _sampleTexture;
    
    public void Dispose() => _sampleTexture?.Dispose();

    public void Initialize()
    {
        _sampleTexture = contentManager.Get<ITexture>("Sample1.jpg");
    }

    public void SetActive(bool active)
    {
    }

    public void Update(float dt)
    {
    }

    public void Draw()
    {
        renderer.Clear(RgbaColor.CornflowerBlue);
        renderer.StateManager.SetTextureFilter(TextureFilter.Point);
        renderer.GeometryRenderer.FillRectangle(new RectangleF(10, 10, 100, 100), RgbaColor.Red);
        renderer.GeometryRenderer.DrawFrame(new RectangleF(10, 10, 100, 100), RgbaColor.Violet, 5);

        for (var idx = 0; idx < 1; ++idx)
        {
            renderer.SpriteRenderer.Draw(
                _sampleTexture.Resource,
                renderer.TargetSize.ToVector() / 2f,
                null,
                _sampleTexture.Resource.Size.ToVector() / 2f,
                timer.RunTime * 45  + idx * 10, 2f);
        }
    }

    public void Resize()
    {
    }
}