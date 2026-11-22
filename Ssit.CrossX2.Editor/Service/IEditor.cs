using System.Collections.Generic;
using System.Numerics;
using SkiaSharp;
using Ssit.CrossX2.Editor.Helpers;
using Ssit.CrossX2.Editor.ViewModels;
using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.Games.Map;

namespace Ssit.CrossX2.Editor.Service;

public interface IEditor
{
    void Redraw();
    MapLayer SelectedLayer { get; set; }
    Vector2 ScreenToMap(Vector2 position);
    Vector2 MapToScreen(Vector2 position);
    Vector2 Offset { get; set; }
    RectangleF DrawEditorImage(EditorImage image, bool flipped, Vector2 position, SKCanvas skCanvas, GRContext grContext, SKPaint skPaint);
    void EnsurePanInBounds();
    ZoomViewModel Zoom { get; }
    IList<SKImage> GetTileSetImages(GRContext context);
    void GetMapObjects(IList<MapObjectInfo> buffer, Vector2 screenPosition);
    /// <param name="containedOnly">Only objects fully inside <paramref name="screenRect"/>; otherwise every object
    /// intersecting it.</param>
    void GetMapObjects(IList<MapObjectInfo> buffer, RectangleF screenRect, bool containedOnly = false);

    /// <summary>The selected object when exactly one is selected; 0 when none or several are selected.
    /// Setting it replaces the whole selection.</summary>
    int SelectedObject { get; set; }

    /// <summary>Ids of all selected objects (multi-selection).</summary>
    IReadOnlyCollection<int> SelectedObjects { get; }
    bool IsObjectSelected(int id);
    void SetSelectedObjects(IEnumerable<int> ids);
    void AddToSelection(int id);
    void RemoveFromSelection(int id);

    bool ShowMaterials { set; }
    bool ShowCollisions { get; }
    void UpdateMousePosition(Vector2? screenPosition);
}