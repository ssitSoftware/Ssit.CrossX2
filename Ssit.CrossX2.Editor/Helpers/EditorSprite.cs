using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Avalonia.Media.Imaging;
using SkiaSharp;
using Ssit.CrossX2.Editor.Service;
using Ssit.CrossX2.Framework.Games.Template;
using Ssit.CrossX2.Framework.Graphics.Sprites;

namespace Ssit.CrossX2.Editor.Helpers;

public class EditorObject: EditorImage
{
    public Type Type { get; }
    public Type ParametersType { get; }
    
    public EditorObject(ISpritesContainer spritesContainer, IGameTemplate gameTemplate, ObjectDescription desc) : base(spritesContainer, gameTemplate, desc)
    {
        Type = desc.TargetType;
        ParametersType = desc.ParametersType;
    }
}

public class EditorImage
{
    public string Name { get; }
    public string Id { get; }
    public Bitmap Preview { get; }

    private readonly HashSet<string> _tags;
    
    public EditorSprite Sprite { get; }
    public string Sequence { get; }
    
    public EditorImage(ISpritesContainer spritesContainer, IGameTemplate gameTemplate, ImageDescription desc)
    {
        Id = desc.FullName;
        Name = desc.Name;
        _tags = new HashSet<string>(desc.Tags);
        Sprite = spritesContainer.Get(desc.GameObject);
        Sequence = desc.Sequence ?? "";
        
        Preview = Sprite.CreateSequencePreview(Sequence, 186, gameTemplate.PreviewZoom);
    }

    public bool HasTags(IEnumerable<string> tags)
    {
        foreach (var tag in tags)
        {
            if (!_tags.Contains(tag))
                return false;
        }

        return true;
    }
}

public class EditorSprite
{
    private readonly SKImage _image;
    public readonly Vector2 Origin;
    private SKImage _hardwareImage;
    
    private readonly Dictionary<string, Sprite.SpriteSequence> _sequences = new();
    private readonly Dictionary<string, float> _sequenceTime = new();

    public EditorSprite(SKImage image, Vector2 origin, Sprite.SpriteSequence[] sequences)
    {
        _image = image;
        Origin = origin;

        foreach (var sequence in sequences)
        {
            float length = 0;

            foreach (var frame in sequence.Frames)
            {
                length += frame.Duration;
            }

            _sequenceTime.Add(sequence.Name, length);
            _sequences.Add(sequence.Name, sequence);
        }
    }

    public Sprite.SpriteFrame GetFrameForTime(string sequenceName, float time)
    {
        if (!_sequences.TryGetValue(sequenceName, out var sequence))
        {
            throw new InvalidDataException();
        }

        time %= _sequenceTime[sequenceName];

        for (var idx = 0; idx < sequence.Frames.Length; ++idx)
        {
            time -= sequence.Frames[idx].Duration;
            if (time <= 0)
            {
                return sequence.Frames[idx];
            }
        }

        return sequence.Frames[0];
    }

    public Bitmap CreateSequencePreview(string name, int maxSize, int previewScale)
    {
        var frame = GetFrameForTime(name, 0);

        var width = frame.Source.Width;
        var height = frame.Source.Height;

        float scale = Math.Min(previewScale, Math.Min((float)maxSize / width, (float)maxSize / height));

        using var bitmap = new SKBitmap(new SKImageInfo((int)(width * scale + 0.95f), (int)(height * scale + 0.95f), SKColorType.Rgba8888));
        using var canvas = new SKCanvas(bitmap);
        
        canvas.Clear(SKColors.Transparent);
        
        canvas.DrawImage(_image, SKRect.Create(frame.Source.X, frame.Source.Y, width, height), SKRect.Create(0, 0, width * scale, height * scale),
            new SKPaint
            {
                FilterQuality = SKFilterQuality.None
            });
        
        var memoryStream = new MemoryStream();
        bitmap.Encode(memoryStream, SKEncodedImageFormat.Png, 0);
        memoryStream.Seek(0, SeekOrigin.Begin);

        return new Bitmap(memoryStream);
    }
    
    public SKImage Get(GRContext context)
    {
        if (_hardwareImage?.Handle is not null)
        {
            return _hardwareImage;
        }

        if (context == null)
        {
            return _image;
        }

        _hardwareImage = _image.ToTextureImage(context);
        return _hardwareImage;
    }
}