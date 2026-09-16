using System.Drawing;
using System.Numerics;

namespace CrossX2.Graphics;

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
    
    void SetPipeline(uint pipeline, object parameters = null);
}