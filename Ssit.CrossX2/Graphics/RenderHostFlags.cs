namespace CrossX2.Graphics;

[Flags]
public enum RenderHostFlags
{
    None = 0,
    EnableGlowPass = 1,
    MatchWidth = 2,
    MatchHeight = 4,
    ExactSize = MatchWidth | MatchHeight
}