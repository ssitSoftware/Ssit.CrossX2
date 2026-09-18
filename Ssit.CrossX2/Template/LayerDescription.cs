using Ssit.CrossX2.Map;

namespace Ssit.CrossX2.Template;

public class LayerDescription
{
    public const string MainLayerId = "MAIN";
    public const string MainLayerName = "Main";
    
    public readonly string Id;
    public readonly string Name;
    public readonly Size Size;

    public readonly float HorizontalSpeed;
    public readonly float VerticalSpeed;

    public readonly string InitialTileset; 
    
    public readonly float Depth;

    public readonly RgbaColor Tint;
    public readonly RgbaColor Fog;

    public readonly LayerAlign Anchor;
    
    public LayerDescription(string id, string name, Size size, 
        float horizontalSpeed, float verticalSpeed, 
        string initialTileset, float depth, 
        RgbaColor tint, LayerAlign anchor, RgbaColor? fog = null)
    {
        Id = id;
        Name = name;
        Size = size;
        HorizontalSpeed = horizontalSpeed;
        VerticalSpeed = verticalSpeed;
        InitialTileset = initialTileset;
        Depth = depth;
        Tint = tint;
        Fog = fog ?? RgbaColor.Transparent;
        Anchor = anchor;
    }
}