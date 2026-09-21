using System.Numerics;
using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Lighting;
using Ssit.CrossX2.Framework.Services;

namespace Samples;

public class GameAppTestComponent(IRenderer renderer, IContentManager contentManager, IAppTimer timer, IRenderHost host) : IAppComponent
{
    private ResourceHandle<ITexture> _sampleTexture;
    
    public void Dispose() => _sampleTexture?.Dispose();

    public void Initialize()
    {
        _sampleTexture = contentManager.Get<ITexture>("assets:/BrickWall2.png");
    }

    public void SetActive(bool active)
    {
    }

    public void Update(float dt)
    {
    }

    private readonly PointLight[] _lights = new PointLight[2];
    private readonly SpotLight[] _spotLights = new SpotLight[1];
    private readonly DirectionalLight[] _directionalLights = new DirectionalLight[1];
    
    public void Draw()
    {
        renderer.Clear(RgbaColor.Black);
        renderer.StateManager.Scale(host.Scale);
        
        _directionalLights[0] = new DirectionalLight( Vector3.Normalize(new Vector3(-1f, 1, 0.25f)), new(0xffffccaa), 0.5f);

        _lights[0] = new PointLight(new Vector3(renderer.TargetSize.ToVector() / 2f - new Vector2(50, 0), -256f), 128, RgbaColor.DarkViolet, 0f);
        _lights[1] = new PointLight(new Vector3(renderer.TargetSize.ToVector() / 2f + Vector2.Transform(new Vector2(96), Matrix3x2.CreateRotation(-timer.RunTime * 2)), -512), 192, RgbaColor.GreenYellow, 1);

        _spotLights[0] = new SpotLight(new Vector3(renderer.TargetSize.ToVector() / 2f - new Vector2(0,  _sampleTexture.Resource.Size.Height * 0.25f), -50),
            Vector2.Normalize(new Vector2(-0.5f, 1)), 30, 15,  800,  RgbaColor.OrangeRed, 3);
        
        renderer.LightingManager.EnableLighting(true);
        renderer.LightingManager.SetPositionsScale(host.Scale);
        renderer.LightingManager.SetAmbientLight(RgbaColor.Navy * 0.2f);
        renderer.LightingManager.SetResolution(0);
        renderer.LightingManager.SetDirectionalLights(_directionalLights);
        //renderer.LightingManager.SetPointLights(_lights);
        renderer.LightingManager.SetSpotLights(_spotLights);
        renderer.LightingManager.SetCellShades(true, 0);

        renderer.StateManager.SetTextureFilter(TextureFilter.Point);
        
        renderer.GeometryRenderer.FillRectangle(new RectangleF(0, 0, renderer.TargetSize.Width, renderer.TargetSize.Height), RgbaColor.Gray);
        renderer.SpriteRenderer.Draw(
            _sampleTexture.Resource,
            renderer.TargetSize.ToVector() / 2 - new Vector2(_sampleTexture.Resource.Size.Width / 2f * 0.35f, 0),
            null,
            _sampleTexture.Resource.Size.ToVector() / 2f, 
            scale: 0.35f);
        renderer.SpriteRenderer.Draw(
            _sampleTexture.Resource,
            renderer.TargetSize.ToVector() / 2 + new Vector2(_sampleTexture.Resource.Size.Width / 2f * 0.35f, 0),
            null,
            _sampleTexture.Resource.Size.ToVector() / 2f, 
            scale: 0.35f, imageTransform: ImageTransform.None);
    }

    public void Resize()
    {
    }
}