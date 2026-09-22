namespace Ssit.CrossX2.Framework.Graphics.Effects;

internal interface IGlowEffect : IDisposable
{
    void Render(IRenderTarget target, ITexture normal, ITexture glow, float sourceScale = 1);
}