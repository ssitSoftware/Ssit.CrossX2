using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.Games.Rendering.Map;

public class TilesDisplaySegment : IDisposable
{
    public class Parameters
    {
        public ReferenceCountedResource<IVertexBuffer> VertexBuffer;
        public int Start;
        public int Count;
        public string TexturePath;
    }
    
    public ResourceHandle<ITexture> Texture { get; }
    public IVertexBuffer VertexBuffer => _vertexBufferRes.Resource;
    
    public int Start { get; }
    public int Count { get; }
    
    private readonly ReferenceCountedResource<IVertexBuffer> _vertexBufferRes;

    public TilesDisplaySegment(IContentManager contentManager, Parameters parameters)
    {
        Texture = contentManager.Get<ITexture>(parameters.TexturePath);
        _vertexBufferRes = parameters.VertexBuffer.Clone();
        
        Start = parameters.Start;
        Count = parameters.Count;
    }

    public void Dispose()
    {
        Texture?.Dispose();
        _vertexBufferRes?.Dispose();
    }
}