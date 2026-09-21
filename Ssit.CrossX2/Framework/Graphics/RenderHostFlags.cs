namespace Ssit.CrossX2.Framework.Graphics;

[Flags]
public enum RenderHostFlags
{
    MatchWidth = 1,
    MatchHeight = 2,
    ExactSize = MatchWidth | MatchHeight,
    PixelPerfect = 4,
    EnableGlowPass = 8,
    EnableCrtSimulation = 16
}