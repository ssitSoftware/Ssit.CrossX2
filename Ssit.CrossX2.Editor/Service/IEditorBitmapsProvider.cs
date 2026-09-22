using SkiaSharp;

namespace Ssit.CrossX2.Editor.Service;

public interface IEditorBitmapsProvider
{
    SKImage[] MaterialsPreview { get; }
}