namespace CrossX2.Graphics;

public class RenderHostParameters
{
    public Size DesignSize { get; set; }
    public RenderHostFlags Flags { get; set; }
    public int MinScale { get; set; } = 1;
    public int MaxScale { get; set; } = 16;
    public uint[] PostProcessingEffects { get; set; } = [];
}