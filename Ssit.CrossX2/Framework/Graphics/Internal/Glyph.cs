using System.Numerics;

namespace Ssit.CrossX2.Framework.Graphics.Internal;

public class Glyph(char c, Rectangle source, Vector2 offset, float advance, Dictionary<char, float> kerning = null)
{
    public char Char { get; } = c;
    public Rectangle Source { get; set; } = source;
    public Vector2 Offset { get; } = offset;
    public float Advance { get; } = advance;
    public float GetKerning(char previousCharacter) => kerning?.GetValueOrDefault(previousCharacter, 0) ?? 0;
    
    internal void Save(BinaryWriter writer)
    {
        writer.Write(Char);
        writer.Write(Source.X);
        writer.Write(Source.Y);
        writer.Write(Source.Width);
        writer.Write(Source.Height);
        writer.Write(Offset.X);
        writer.Write(Offset.Y);
        writer.Write(Advance);

        writer.Write(kerning?.Count ?? 0);

        if (kerning?.Count > 0)
        {
            foreach (var kvp in kerning)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
        }
    }

    internal static Glyph Read(BinaryReader reader)
    {
        var c = reader.ReadChar();

        if (c == 0)
        {
            return null;
        }
        
        var sourceX = reader.ReadInt32();
        var sourceY = reader.ReadInt32();
        var sourceW = reader.ReadInt32();
        var sourceH = reader.ReadInt32();
        
        var offsetX = reader.ReadSingle();
        var offsetY = reader.ReadSingle();
        var advance = reader.ReadSingle();

        var kerningCount = reader.ReadInt32();

        Dictionary<char, float> kerning = null;

        if (kerningCount > 0)
        {
            kerning = new Dictionary<char, float>();
            for (int i = 0; i < kerningCount; i++)
            {
                var ch = reader.ReadChar();
                var kern = reader.ReadSingle();
                kerning.Add(ch, kern);
            }
        }

        return new Glyph(c, new Rectangle(sourceX, sourceY, sourceW, sourceH), new Vector2(offsetX, offsetY), advance, kerning);
    }
}