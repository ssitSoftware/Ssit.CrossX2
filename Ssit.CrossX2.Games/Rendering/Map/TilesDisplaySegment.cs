using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.Games.Rendering.Map;

public class TilesDisplaySegment(IContentManager contentManager, TilesDisplaySegment.Parameters parameters)
    : IDisposable
{
    public class Parameters
    {
        public IReadOnlyList<Quad> Quads;
        public string TexturePath;
    }
    
    public ResourceHandle<ITexture> Texture { get; } = contentManager.Get<ITexture>(parameters.TexturePath);
    public IReadOnlyList<Quad> Quads { get; } = parameters.Quads;

    public void Dispose()
    {
        Texture?.Dispose();
    }
}