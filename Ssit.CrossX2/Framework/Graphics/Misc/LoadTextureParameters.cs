namespace Ssit.CrossX2.Framework.Graphics.Misc;

public class LoadTextureParameters
{
    public Stream DiffuseMapStream { get; init; }
    public Stream NormalMapStream { get; init; }
    public Stream GlowMapStream { get; init; }
    public bool GlowFromDiffuse { get; init; }
}
