using System.Numerics;
using Ssit.CrossX2.Framework.Graphics.Sprites;

namespace Ssit.CrossX2.Framework.Graphics.Renderers;

public class SpriteRendererImpl(IRenderQueue renderQueue): ISpriteRenderer
{
    public void Draw(ITexture texture, RectangleF target, RectangleF? sourceRectangle = null, Vector2? origin = null, float rotation = 0,
        RgbaColor? nullableColor = null, ImageTransform imageTransform = ImageTransform.None, float depth = 0)
    {
        var source = sourceRectangle ?? new RectangleF(0, 0, texture.Size.Width, texture.Size.Height);
        var color = nullableColor ?? RgbaColor.White;
        var originValue = origin ?? Vector2.Zero;
     
        if ((imageTransform & ImageTransform.FlipHorizontal) != 0)
        {
            originValue.X = source.Width - originValue.X;
        }
        
        if ((imageTransform & ImageTransform.FlipVertical) != 0)
        {
            originValue.Y = source.Height - originValue.Y;
        }
        
        var scaleX = source.Width != 0 ? target.Width / source.Width : 1f;
        var scaleY = source.Height != 0 ? target.Height / source.Height : 1f;

        var localTl = new Vector2(-originValue.X * scaleX, -originValue.Y * scaleY);
        var localTr = new Vector2((source.Width - originValue.X) * scaleX, -originValue.Y * scaleY);
        var localBl = new Vector2(-originValue.X * scaleX, (source.Height - originValue.Y) * scaleY);
        var localBr = new Vector2((source.Width - originValue.X) * scaleX, (source.Height - originValue.Y) * scaleY);

        var anchor = target.TopLeft;

        Vector2 posTl, posTr, posBl, posBr;

        if (rotation == 0)
        {
            posTl = anchor + localTl;
            posTr = anchor + localTr;
            posBl = anchor + localBl;
            posBr = anchor + localBr;
        }
        else
        {
            var radians = rotation * (MathF.PI / 180f);
            var sin = MathF.Sin(radians);
            var cos = MathF.Cos(radians);

            posTl = anchor + Rotate(localTl, sin, cos);
            posTr = anchor + Rotate(localTr, sin, cos);
            posBl = anchor + Rotate(localBl, sin, cos);
            posBr = anchor + Rotate(localBr, sin, cos);
        }

        var (uvTl, uvTr, uvBl, uvBr) = GetTextureCoordinates(source, texture.Size, imageTransform);

        var vTl = new VertexPct(new Vector3(posTl, depth), color, uvTl);
        var vTr = new VertexPct(new Vector3(posTr, depth), color, uvTr);
        var vBl = new VertexPct(new Vector3(posBl, depth), color, uvBl);
        var vBr = new VertexPct(new Vector3(posBr, depth), color, uvBr);

        renderQueue.PushTriangle(vTl, vBl, vBr, texture);
        renderQueue.PushTriangle(vTl, vBr, vTr, texture);
    }

    public void Draw(ITexture texture, Vector2 position, RectangleF? sourceRectangle = null, Vector2? origin = null, float rotation = 0,
        float scale = 1, RgbaColor? color = null, ImageTransform imageTransform = ImageTransform.None, float depth = 0)
    {
        var sourceRect = sourceRectangle ?? new RectangleF(0, 0, texture.Size.Width, texture.Size.Height);
        
        var targetRect = new RectangleF(position.X, position.Y, sourceRect.Width * scale, sourceRect.Height * scale);
        Draw(texture, targetRect, sourceRect, origin, rotation, color, imageTransform);
    }

    public void Draw(SpriteInstance sprite, Vector2 position, float rotation = 0, float scale = 1, RgbaColor? color = null, ImageTransform transform = ImageTransform.None, float depth = 0) 
        => Draw(sprite.SpriteSheet, position, sprite.Source, sprite.Origin, rotation, scale, color, transform, depth);

    private static Vector2 Rotate(Vector2 v, float sin, float cos) => new(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);

    private static (Vector2 tl, Vector2 tr, Vector2 bl, Vector2 br) GetTextureCoordinates(RectangleF source, Size textureSize, ImageTransform transform)
    {
        var u0 = source.X / textureSize.Width;
        var v0 = source.Y / textureSize.Height;
        var u1 = source.Right / textureSize.Width;
        var v1 = source.Bottom / textureSize.Height;

        if ((transform & ImageTransform.FlipHorizontal) != 0)
            (u0, u1) = (u1, u0);

        if ((transform & ImageTransform.FlipVertical) != 0)
            (v0, v1) = (v1, v0);

        var tl = new Vector2(u0, v0);
        var tr = new Vector2(u1, v0);
        var bl = new Vector2(u0, v1);
        var br = new Vector2(u1, v1);

        return (tl, tr, bl, br);
    }
}