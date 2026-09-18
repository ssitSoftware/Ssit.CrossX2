using Ssit.CrossX2.Utils;

namespace Ssit.CrossX2.Graphics;

public static class RendererComponents
{
    public static readonly ComponentId GeometryPipeline = Ids.Get(nameof(GeometryPipeline));
    public static readonly ComponentId TexturePipeline = Ids.Get(nameof(TexturePipeline));
    public static readonly ComponentId TextureWithLightingPipeline = Ids.Get(nameof(TextureWithLightingPipeline));

    public static readonly ComponentId BloomEffect = Ids.Get(nameof(BloomEffect));
    public static readonly ComponentId CrtSimulationEffect = Ids.Get(nameof(CrtSimulationEffect));
}