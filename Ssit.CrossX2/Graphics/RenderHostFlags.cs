namespace Ssit.CrossX2.Graphics;

[Flags]
public enum RenderHostFlags
{
    MatchWidth = 1,
    MatchHeight = 2,
    ExactSize = MatchWidth | MatchHeight,
    EnableGlowPass = 4,
    EnableCrtSimulation = 8
}