using System.Numerics;
using Newtonsoft.Json;
using Ssit.CrossX2.Framework.IO;

namespace Ssit.CrossX2.Framework.Graphics.Sprites.Json;

public static class JsonSpriteLoader
{
    public static IDisposable Load(string path, IFilesProvider filesProvider)
    {
        return LoadInternal(path, filesProvider);
    }

    internal static Sprite LoadInternal(string path, IFilesProvider filesProvider)
    {
        using var spriteStream = filesProvider.Open(path);
        var data = new StreamReader(spriteStream).ReadToEnd();
        var jsonSprite = JsonConvert.DeserializeObject<JsonSprite>(data);
        
        var dict = new Dictionary<string, List<(int, Sprite.SpriteFrame)>>();

        Size sourceSize = Size.Zero;
        
        foreach (var frame in jsonSprite.Frames)
        {
            if (!dict.TryGetValue(frame.Sequence, out var list))
            {
                list = new List<(int, Sprite.SpriteFrame)>();
                dict.Add(frame.Sequence, list);
            }
            
            if ( sourceSize == Size.Zero)
            {
                sourceSize = new Size(frame.SourceSize.W, frame.SourceSize.H);
            }
            
            list.Add((frame.Index, CreateFrame(frame)));
        }
        
        var sequences = new List<Sprite.SpriteSequence>();

        foreach (var sequence in dict)
        {
            sequence.Value.Sort((a, b) => a.Item1.CompareTo(b.Item1));
            
            var frames = new Sprite.SpriteFrame[sequence.Value.Count];
            for (var idx = 0; idx < sequence.Value.Count; idx++)
            {
                frames[idx] = sequence.Value[idx].Item2;
            }
            sequences.Add(new Sprite.SpriteSequence(sequence.Key, frames));
        }

        var addedPersistent = path.EndsWith('!') ? "!" : "";
            
        var sheetDir = Path.GetDirectoryName(path);
        var sheetFile = Path.GetFileNameWithoutExtension(path) + ".png" + addedPersistent;
        var sheetPath = Path.Combine(sheetDir ?? "", sheetFile);
        return new Sprite(sheetPath, sequences, sourceSize);
    }

    private static Sprite.SpriteFrame CreateFrame(JsonFrame frame)
    {
        var duration = frame.Duration / 1000f;
        
        Vector2 offset = Vector2.Zero;
        offset.X -= frame.SourceRect.X;
        offset.Y -= frame.SourceRect.Y;

        var source = new Rectangle(
            frame.FrameRect.X,
            frame.FrameRect.Y,
            frame.FrameRect.W,
            frame.FrameRect.H);
        
        return new Sprite.SpriteFrame(source, offset, duration);
    }
}