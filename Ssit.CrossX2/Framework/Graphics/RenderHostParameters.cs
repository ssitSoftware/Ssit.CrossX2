using Ssit.CrossX2.Framework.Utils;

namespace Ssit.CrossX2.Framework.Graphics;

internal class RenderHostParameters: BindableModel, IRenderHostParameters
{
    public Size DesignSize
    {
        get;
        set => SetField(ref field, value);
    } = new(1280, 720);

    public RenderHostFlags Flags
    {
        get;
        set => SetField(ref field, value);
    } = RenderHostFlags.MatchHeight;

    public int MinScale
    {
        get;
        set => SetField(ref field, value);
    } = 1;

    public int MaxScale
    {
        get;
        set => SetField(ref field, value);
    } = 32;

    public IPostRenderer PostRenderer { get; set; }
}