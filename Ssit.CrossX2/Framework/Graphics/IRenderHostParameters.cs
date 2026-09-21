namespace Ssit.CrossX2.Framework.Graphics;

public interface IRenderHostParameters
{
    Size DesignSize { get; set; }
    RenderHostFlags Flags { get; set; }
    int MinScale { get; set; }
    int MaxScale { get; set; }
}