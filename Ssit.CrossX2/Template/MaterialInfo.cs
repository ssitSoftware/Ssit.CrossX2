namespace Ssit.CrossX2.Template;

public class MaterialInfo
{
    public MaterialInfo(string name, string shortName, RgbaColor previewColor, RgbaColor debugColor)
    {
        Name = name;
        ShortName = shortName;
        PreviewColor = previewColor;
        DebugColor = debugColor;
    }

    public string Name { get; }
    public string ShortName { get; }
    public RgbaColor PreviewColor { get; }
    public RgbaColor DebugColor { get; }
}