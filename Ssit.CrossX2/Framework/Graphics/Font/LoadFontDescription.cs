namespace Ssit.CrossX2.Framework.Graphics.Font;

public class LoadFontDescription
{
    public string TtfPath { get; set; }
    public string TtfName { get; set; }
    public int[] Sizes { get; set; }
    public bool Antialiasing { get; set; }
    public CharSets CharSets { get; set; }
    public int Outline { get; set; }
    public int Spacing { get; set; }
    public float SheetWidthFactor { get; set; } = 16;
}