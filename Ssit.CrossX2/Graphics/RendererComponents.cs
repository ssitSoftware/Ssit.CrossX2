namespace CrossX2.Graphics;

public static class RendererComponents
{
    public static readonly uint GeometryPipeline = Ids.Get(nameof(GeometryPipeline));
    public static readonly uint TexturePipeline = Ids.Get(nameof(TexturePipeline));
    public static readonly uint TextureWithLightingPipeline = Ids.Get(nameof(TextureWithLightingPipeline));

    public static readonly uint BloomEffect = Ids.Get(nameof(BloomEffect));
    public static readonly uint CrtSimulationEffect = Ids.Get(nameof(CrtSimulationEffect));
}