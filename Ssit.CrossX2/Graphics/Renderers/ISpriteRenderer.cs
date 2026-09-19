using System.Numerics;
using Ssit.CrossX2.Graphics.Sprites;

namespace Ssit.CrossX2.Graphics.Renderers;

public interface ISpriteRenderer
{
    void Draw(ITexture texture, RectangleF target, RectangleF? sourceRectangle = null, Vector2? origin = null, float rotation = 0,
        RgbaColor? nullableColor = null, ImageTransform imageTransform = ImageTransform.None);
    
    void Draw(ITexture texture, Vector2 position, RectangleF? sourceRectangle = null,
        Vector2? origin = null, float rotation = 0, float scale = 1, RgbaColor? color = null,
        ImageTransform imageTransform = ImageTransform.None);

    void Draw(SpriteInstance sprite, Vector2 position, float rotation = 0, float scale = 1, RgbaColor? color = null,
        ImageTransform transform = ImageTransform.None);
}