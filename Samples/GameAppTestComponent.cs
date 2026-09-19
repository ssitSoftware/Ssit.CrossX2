using System.Numerics;
using Ssit.CrossX2;
using Ssit.CrossX2.Content;
using Ssit.CrossX2.Core;
using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.Graphics.Lighting;
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

    private readonly PointLight2D[] _lights = new PointLight2D[2];
    
    public void Draw()
    {
        _lights[0] = new PointLight2D(renderer.TargetSize.ToVector() / 2f, 256, RgbaColor.Orange, 1);
        _lights[1] = new PointLight2D(renderer.TargetSize.ToVector() / 2f + Vector2.Transform(new Vector2(512, 0), Matrix3x2.CreateRotation(-timer.RunTime * 2)), 512, RgbaColor.Orange, 3);

        renderer.Clear(RgbaColor.CornflowerBlue);

        renderer.LightingManager.EnableLighting(true);
        renderer.LightingManager.SetAmbientLight(RgbaColor.Gray);
        renderer.LightingManager.SetResolution(0);
        renderer.LightingManager.SetPointLights(_lights);

        renderer.StateManager.SetTextureFilter(TextureFilter.Point);
        
        renderer.GeometryRenderer.FillRectangle(new RectangleF(0, 0, renderer.TargetSize.Width, renderer.TargetSize.Height), new RgbaColor(10,10,10));
        
        renderer.SpriteRenderer.Draw(
            _sampleTexture.Resource,
            renderer.TargetSize.ToVector() / 2f,
            null,
            _sampleTexture.Resource.Size.ToVector() / 2f,
            timer.RunTime * 45, 2f);
    }

    public void Resize()
    {
    }
}