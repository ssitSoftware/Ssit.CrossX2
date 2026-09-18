namespace Ssit.CrossX2.UI.Values;

public struct FontDesc
{
    public string FontFamily { get; set; }
    public int FontSize { get; set; }
    
    private FontDesc(string fontFamily, int fontSize)
    {
        FontFamily = fontFamily;
        FontSize = fontSize;
    }
    
    public static implicit operator FontDesc((string fontFamily, int fontSize) info) => new(info.fontFamily, info.fontSize);  
    public static implicit operator FontDesc(string fontFamily) => new(fontFamily, 0);
}