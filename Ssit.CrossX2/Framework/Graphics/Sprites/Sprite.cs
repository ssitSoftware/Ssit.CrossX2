using System.Numerics;

namespace Ssit.CrossX2.Framework.Graphics.Sprites;

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

    /// <summary>
    /// Returns a sprite that additionally contains sequences built from frame ranges of existing sequences.
    /// </summary>
    internal Sprite WithDerivedSequences(IReadOnlyList<Json.JsonSpriteSequenceDescription> derived)
    {
        if (derived is null || derived.Count == 0)
            return this;

        var sequences = new List<SpriteSequence>(_sequences.Values);

        foreach (var desc in derived)
        {
            if (!_sequences.TryGetValue(desc.Source, out var source))
                throw new Exception($"Derived sequence '{desc.Name}': source sequence '{desc.Source}' not found in {SheetName}");

            var start = Math.Clamp(desc.Start, 0, source.Frames.Length - 1);
            var count = desc.Count > 0 ? Math.Min(desc.Count, source.Frames.Length - start) : source.Frames.Length - start;

            sequences.RemoveAll(o => o.Name == desc.Name);
            sequences.Add(new SpriteSequence(desc.Name, source.Frames.Skip(start).Take(count).ToArray()));
        }

        return new Sprite(SheetName, sequences, SourceSize);
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