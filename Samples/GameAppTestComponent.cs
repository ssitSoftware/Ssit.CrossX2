using System.Numerics;
using Ssit.CrossX2;
using Ssit.CrossX2.Content;
using Ssit.CrossX2.Core;
using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.Graphics.Lighting;
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
        _lighting -= dt * 33;
        _lighting =  MathF.Max(_lighting, 0);
        
        if (Random.Shared.NextDouble() < 0.01)
        {
            _lighting = 0;
        }
    }

    private readonly PointLight[] _lights = new PointLight[2];
    private readonly SpotLight[] _spotLights = new SpotLight[1];
    private float _lighting;
    
    public void Draw()
    {
        _lights[0] = new PointLight(new Vector3(renderer.TargetSize.ToVector() / 2f - new Vector2(200, 0), -512f), 512, RgbaColor.Green, 1.5f);
        _lights[1] = new PointLight(new Vector3(renderer.TargetSize.ToVector() / 2f + Vector2.Transform(new Vector2(512), Matrix3x2.CreateRotation(-timer.RunTime * 2)), -1024), 1024, RgbaColor.Yellow, 2);
        
        _spotLights[0] = new SpotLight(new Vector3(renderer.TargetSize.ToVector() / 2f - new Vector2(0, _sampleTexture.Resource.Size.Height / 1.5f), -1000),
            Vector2.Normalize(Vector2.Transform(new Vector2(1, 0), Matrix3x2.CreateRotation(timer.RunTime * 3.3f))), 30, 15,  1000,  RgbaColor.Red, 1);
        
        renderer.StateManager.Reset();

        renderer.LightingManager.EnableLighting(true);
        renderer.LightingManager.SetAmbientLight(new RgbaColor(30,30,30).Mix(RgbaColor.White, _lighting));
        renderer.LightingManager.SetResolution(12);
        renderer.LightingManager.SetPointLights(_lights);
        renderer.LightingManager.SetSpotLights(_spotLights);
        renderer.LightingManager.SetCellShades(true, 8);

        renderer.StateManager.SetTextureFilter(TextureFilter.Point);
        
        renderer.GeometryRenderer.FillRectangle(new RectangleF(0, 0, renderer.TargetSize.Width, renderer.TargetSize.Height), RgbaColor.LightSeaGreen);
        renderer.SpriteRenderer.Draw(
            _sampleTexture.Resource,
            renderer.TargetSize.ToVector() / 2,
            null,
            _sampleTexture.Resource.Size.ToVector() / 2f, scale: 3f);
    }

    public void Resize()
    {
    }
}