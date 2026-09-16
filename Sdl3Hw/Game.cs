using CrossX2;
using CrossX2.Graphics;
using CrossX2.Graphics.Effects;
using CrossX2.Graphics.Pipelines;
using static CrossX2.Graphics.RendererComponents;

namespace Sdl3Hw;

public class Game
{
    private readonly Lighting2DParameters _lightingParameters = new();
    
    protected void Init(IGameRendererBuilder builder)
    {
        builder
            .WithComponent(GeometryPipeline)
            .WithComponent(TexturePipeline)
            .WithComponent(TextureWithLightingPipeline)
            .WithComponent(BloomEffect, new BloomEffectParameters
            {
                Threshold = 0.5f,
                Intensity = 0.5f,
            })
            .WithComponent(CrtSimulationEffect, new CrtSimulationEffectParameters
            {
                BarrelDistortion = 0.02f,
                RgbDisplacement = 0.25f,
                ScanlineIntensity = 0.2f,
                Vignette = 0.25f,
            })
            .WithRenderHost(
                new RenderHostParameters
                {
                    DesignSize = new Size(640, 360),
                    Flags = RenderHostFlags.EnableGlowPass | RenderHostFlags.ExactSize,
                    MinScale = 1,
                    MaxScale = 16,
                    PostProcessingEffects = [BloomEffect, CrtSimulationEffect]
                });
    }

    protected void Render(IRenderer renderer)
    {
        renderer.StateManager.SetPipeline(TextureWithLightingPipeline, _lightingParameters);
        
    }
}