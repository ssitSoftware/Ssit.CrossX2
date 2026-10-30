using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.Games.HexMap;

public class HexTileset: IDisposable
{
    public ITexture Texture { get; }
    public HexTile[] Tiles { get; }
    public Size TileOffset { get; }

    internal HexTileset(ITexture texture, HexTile[] tiles, Size tileOffset)
    {
        Texture = texture;
        Tiles = tiles;
        TileOffset = tileOffset;
    }
    
    public void Dispose() => Texture?.Dispose();
}