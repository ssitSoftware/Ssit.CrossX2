namespace Ssit.CrossX2.Graphics;

public interface ITexture: IDisposable
{
    TextureMaps Maps { get; }
    Size Size { get; }
}