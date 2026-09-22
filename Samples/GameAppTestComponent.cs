using System.Numerics;
using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Lighting;
using Ssit.CrossX2.Framework.Input;
using Ssit.CrossX2.Framework.Services;

namespace Samples;

public class GameAppTestComponent(IRenderer renderer, IContentManager contentManager, IAppTimer timer, IRenderHost host, IKeyboard keyboard) : IAppComponent
{
    private ResourceHandle<ITexture> _sampleTexture;
    private ResourceHandle<ITexture> _redHood;

    private bool _enableBumpMapping = true;
    
    public void Dispose()
    {
        _sampleTexture?.Dispose();
        _redHood?.Dispose();
    }

    public void Initialize()
    {
        _sampleTexture = contentManager.Get<ITexture>("assets:/BrickWall2.png");
        _redHood = contentManager.Get<ITexture>("assets:/RedHood.png");
    }

    public void SetActive(bool active)
    {
    }

    public void Update(float dt)
    {
        if (keyboard.GetKey(Key.B) == ButtonState.JustPressed)
        {
            _enableBumpMapping = !_enableBumpMapping;
        }
    }

    private readonly PointLight[] _lights = new PointLight[2];
    private readonly SpotLight[] _spotLights = new SpotLight[2];
    private readonly DirectionalLight[] _directionalLights = new DirectionalLight[2];
    

    public void Draw()
    {
        renderer.Clear(RgbaColor.Black);
        renderer.StateManager.Scale(host.Scale);

        if (renderer.CurrentPass == RenderPass.Normal)
        {
            _directionalLights[0] = new DirectionalLight(new Vector3(1,-1,-0.5f), RgbaColor.Green, 1.5f);
            _directionalLights[1] = new DirectionalLight(new Vector3(-1,1,-0.5f), RgbaColor.Green, 1.5f);
            
            _lights[0] = new PointLight(new Vector3(host.LogicalSize.ToVector() / 2f - new Vector2(50, 0), -256f), 128, RgbaColor.DarkViolet, 1f);
            _lights[1] = new PointLight(
                new Vector3(host.LogicalSize.ToVector() / 2f + Vector2.Transform(new Vector2(32), Matrix3x2.CreateRotation(-timer.RunTime * 2)), -512), 192,
                RgbaColor.GreenYellow, 1f);

            _spotLights[0] = new SpotLight(new Vector3(host.LogicalSize.ToVector() / 2f - new Vector2(-200, _sampleTexture.Resource.Size.Height * 0.2f), -60),
                Vector2.Normalize(new Vector2(-0.5f, 1)), 30, 15, 3000, RgbaColor.White, 1.4f);
            
            _spotLights[1] = new SpotLight(new Vector3(host.LogicalSize.ToVector() / 2f - new Vector2(200, _sampleTexture.Resource.Size.Height * 0.2f), -60),
                Vector2.Normalize(new Vector2(0.5f, 1)), 45, 25, 3000, RgbaColor.White, 1.4f);

            renderer.LightingManager.EnableLighting(true, _enableBumpMapping);
            renderer.LightingManager.SetAmbientLight(RgbaColor.Gray * 0.5f);
            renderer.LightingManager.SetResolution(2);
            //renderer.LightingManager.SetDirectionalLights(_directionalLights);
            //renderer.LightingManager.SetPointLights(_lights);
            renderer.LightingManager.SetSpotLights(_spotLights);
            renderer.LightingManager.SetCellShades(true, 0);
        }

        renderer.StateManager.SetTextureFilter(TextureFilter.Point);
        
        renderer.GeometryRenderer.FillRectangle(new RectangleF(0, 0, host.LogicalSize.Width, host.LogicalSize.Height), renderer.CurrentPass == RenderPass.Normal ? new RgbaColor(96,96,128) : RgbaColor.Black);

        renderer.SpriteRenderer.Draw(_redHood.Resource, host.LogicalSize.ToVector() / 2, null, _redHood.Resource.Size.ToVector() / 2f,
            imageTransform: ImageTransform.FlipHorizontal);
        
        // renderer.GeometryRenderer.FillRectangle(new RectangleF(0, 0, host.LogicalSize.Width, host.LogicalSize.Height), RgbaColor.Gray);
        // renderer.SpriteRenderer.Draw(
        //     _sampleTexture.Resource,
        //     host.LogicalSize.ToVector() / 2 - new Vector2(_sampleTexture.Resource.Size.Width / 2f * 0.27f, 0),
        //     null,
        //     _sampleTexture.Resource.Size.ToVector() / 2f, 
        //     scale: 0.27f, color: renderer.CurrentPass == RenderPass.Normal ? RgbaColor.White : RgbaColor.Blue);
        //
        // renderer.SpriteRenderer.Draw(
        //     _sampleTexture.Resource,
        //     host.LogicalSize.ToVector() / 2 + new Vector2(_sampleTexture.Resource.Size.Width / 2f * 0.27f, 0),
        //     null,
        //     _sampleTexture.Resource.Size.ToVector() / 2f, 
        //     scale: 0.27f, imageTransform: ImageTransform.None);
    }

    public void Resize()
    {
    }
}