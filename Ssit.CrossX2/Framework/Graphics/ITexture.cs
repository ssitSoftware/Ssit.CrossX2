namespace Ssit.CrossX2.Framework.Graphics;

public interface ITexture: IDisposable
{
    TextureMaps Maps { get; }
    Size Size { get; }
}