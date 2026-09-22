using System.Collections.Generic;
using System.IO;
using System.Numerics;
using SkiaSharp;
using Ssit.CrossX2.Editor.Helpers;
using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.Games;
using Ssit.CrossX2.Framework.Games.Template;
using Ssit.CrossX2.Framework.Graphics.Sprites;
using Ssit.CrossX2.Framework.Graphics.Sprites.Json;

namespace Ssit.CrossX2.Editor.Service;

public class SpritesContainer(IGameTemplate gameTemplate) : ISpritesContainer
{
    private readonly Dictionary<string, EditorSprite> _sprites = new();

    public void Load()
    {
        foreach (var obj in gameTemplate.Objects)
        {
            LoadSprite(obj.GameObject);
        }
        
        foreach (var img in gameTemplate.Images)
        {
            LoadSprite(img.GameObject);
        }
    }

    private void LoadSprite(string path)
    {
        if (_sprites.ContainsKey(path)) return;

        var go = GameObject.FromPath(path, gameTemplate.AssetsProvider, gameTemplate.ObjectsOriginAlignment);
        
        if (go.HasSprite)
        {
            var sprite = (Sprite)JsonSpriteLoader.Load(go.ResourcePath, gameTemplate.AssetsProvider);
            using var stream = gameTemplate.AssetsProvider.Open(sprite.SheetName);

            using var bmp = SKBitmap.Decode(stream);
            var image = SKImage.FromBitmap(bmp);
                
            _sprites.Add(path, new EditorSprite(image, go.Description.Origin, sprite.Sequences));
        }
        else if (go.HasTexture)
        {
            using var stream = gameTemplate.AssetsProvider.Open(go.ResourcePath);
            using var bmp = SKBitmap.Decode(stream);
            var image = SKImage.FromBitmap(bmp);
            
            var sequences = new[]
            {
                new Sprite.SpriteSequence("", [
                    new Sprite.SpriteFrame(new Rectangle(0, 0, image.Width, image.Height), Vector2.Zero, 1)
                ])
            };
            
            _sprites.Add(path, new EditorSprite(image, go.Description.Origin, sequences));
        }
        else throw new InvalidDataException($"GameObject {path} has no sprite.");
    }

    public EditorSprite Get(string name)
    {
        _sprites.TryGetValue(name, out var sprite);
        return sprite;
    }
}