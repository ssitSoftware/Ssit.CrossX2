namespace Ssit.CrossX2.Framework.Graphics.Effects;

internal interface ICrtSimEffect: IDisposable
{
    void Render(IRenderTarget target, ITexture source, float sourceScale);
}