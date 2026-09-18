using System.Numerics;

namespace Ssit.CrossX2.Graphics.Sprites;

public class Sprite: IDisposable
{
    public class SpriteFrame(Rectangle source, Vector2 offset, float duration)
    {
        public Rectangle Source { get; } = source;
        public Vector2 Offset { get; } = offset;
        public float Duration { get; } = duration;
    }
    
    public class SpriteSequence(string name, SpriteFrame[] frames)
    {
        public string Name { get; } = name;
        public SpriteFrame[] Frames { get; } = frames;
    }
    
    public string SheetName { get; }
    public Size SourceSize { get; }
    
    private readonly IReadOnlyDictionary<string, SpriteSequence> _sequences;

    public SpriteSequence[] Sequences => _sequences.Values.ToArray();
    
    private Sprite()
    {
    }

    internal Sprite(string sheetName, IReadOnlyList<SpriteSequence> sequences, Size sourceSize)
    {
        SheetName = sheetName;
        SourceSize = sourceSize;
        var dict = new Dictionary<string, SpriteSequence>();
        foreach (var sequence in sequences)
        {
            dict.Add(sequence.Name, sequence);
        }
        _sequences = dict;
    }

    public SpriteSequence GetSequence(string sequenceName)
    {
        return _sequences[sequenceName];
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}