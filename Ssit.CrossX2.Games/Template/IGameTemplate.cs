using System.Numerics;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.IO;

namespace Ssit.CrossX2.Framework.Games.Template;

public interface IGameTemplate
{
    Vector2 Gravity { get; }
    string Name { get; }
    Guid Guid { get; }
    int TileSize { get; }
    
    bool DisplayScreenGridVertical => false;
    
    RgbaColor GameBackground { get; }
    RgbaColor EditorLayerBackground => GameBackground;
    
    LayerDescription[] Layers { get; }
    ObjectDescription[] Objects { get; }
    ImageDescription[] Images { get; }

    string[] TileSets { get; }
    ContentAlign ObjectsOriginAlignment { get; }
    
    MaterialInfo[] Materials { get; }
    IFilesProvider AssetsProvider { get; }
    decimal TilesetPanelZoom { get; }
    int PreviewZoom { get; }
    RgbaColor EmptyColor { get; }
    RgbaColor TilesBgColor => EmptyColor;
    Size TargetSize { get; }
}