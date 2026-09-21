using System.Numerics;

namespace Ssit.CrossX2.Framework.Graphics.Renderers;

public interface IPrimitiveRenderer
{
    void RenderVertices(PrimitiveType type, IVertexBuffer vertices, int start, int count, ITexture texture = null, Matrix4x4? transform = null);
}