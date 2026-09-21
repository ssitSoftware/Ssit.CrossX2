using System.Numerics;

namespace Ssit.CrossX2.Framework.Graphics;

public interface IStateManager
{
    void SaveState();
    void RestoreState();
    void Reset();

    void Scale(float scale);
    void Translate(Vector2 offset);
    void SetBlendMode(BlendMode blendMode);
    void SetTextureFilter(TextureFilter filter);
    void SetClipRect(RectangleF? clipRect, bool intersectExisting = true);
}