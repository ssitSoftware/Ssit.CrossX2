namespace Ssit.CrossX2.Framework.Graphics.Sprites.Json;

/// <summary>
/// Sequence derived from a range of frames of another sequence (defined in .desc.json "Sequences").
/// Count &lt;= 0 means "up to the end of the source sequence".
/// </summary>
public class JsonSpriteSequenceDescription(string name, string source, int start, int count)
{
    public string Name { get; } = name;
    public string Source { get; } = source;
    public int Start { get; } = start;
    public int Count { get; } = count;
}
