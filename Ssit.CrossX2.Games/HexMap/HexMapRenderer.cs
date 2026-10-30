using System.Numerics;
using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.Games.HexMap;

public class HexMapRenderer : IDisposable
{
    private readonly ResourceHandle<HexTileset> _tileset;

    public HexMapRenderer(IContentManager contentManager, string tilesetPath)
    {
        _tileset = contentManager.Get<HexTileset>(tilesetPath);
    }

    /// <summary>
    /// Renders the flat-top hex grid visible within <paramref name="bounds"/>, with the tile at the
    /// offset coordinate <paramref name="position"/> placed at the center of <paramref name="bounds"/>.
    /// Tiles are positioned directly from their (col, row) offset coordinate using the tileset's
    /// authored <see cref="HexTileset.TileOffset"/>: that is the column-to-column and row-to-row
    /// pixel step, with odd columns additionally shifted down by half the row step.
    /// <paramref name="scale"/> is applied to the tileset's offset to convert it to screen pixels.
    /// Each hex draws the tile image for its tile value, read from <paramref name="tiles"/> by its
    /// (col, row) offset coordinate; hexes with no entry in <paramref name="tiles"/> are skipped.
    /// </summary>
    public void Render(IRenderer renderer, Vector2 position, float scale, RectangleF bounds, IReadOnlyDictionary<(int col, int row), int> tiles)
    {
        var tileset = _tileset.Resource;
        var tileOffset = new Vector2(tileset.TileOffset.Width, tileset.TileOffset.Height) * scale;

        var centerPixel = OffsetToPixelAvg(position.X, position.Y, tileOffset);
        var offset = bounds.Center - centerPixel;

        var halfColumns = (int)MathF.Ceiling(bounds.Width / tileOffset.X / 2f) + 1;
        var halfRows = (int)MathF.Ceiling(bounds.Height / tileOffset.Y / 2f) + 1;

        var centerCol = (int)MathF.Round(position.X);
        var centerRow = (int)MathF.Round(position.Y);

        renderer.StateManager.SaveState();
        renderer.StateManager.Translate(offset);

        for (var row = centerRow - halfRows; row <= centerRow + halfRows; row++)
        {
            // Even columns sit higher (further back) than odd columns within the same row, so they
            // must be drawn first - otherwise a later-drawn even column paints over an odd column's
            // tile artwork that extends above its footprint.
            for (var parity = 0; parity <= 1; parity++)
            {
                for (var col = centerCol - halfColumns; col <= centerCol + halfColumns; col++)
                {
                    if ((col & 1) != parity)
                    {
                        continue;
                    }

                    if (!tiles.TryGetValue((col, row), out var tileIndex))
                    {
                        continue;
                    }

                    var tile = tileset.Tiles[tileIndex];
                    var hexCenter = OffsetToPixel(col, row, tileOffset);
                    var spriteSize = new Vector2(tile.Source.Width, tile.Source.Height) * scale;

                    if (!bounds.IsIntersecting(new RectangleF(hexCenter + offset - spriteSize, new SizeF(spriteSize.X * 2, spriteSize.Y * 2))))
                    {
                        continue;
                    }

                    renderer.SpriteRenderer.Draw(tileset.Texture, hexCenter, tile.Source, tile.Origin, scale: scale);
                }
            }
        }

        renderer.StateManager.RestoreState();
    }

    private static Vector2 OffsetToPixel(float col, float row, Vector2 tileOffset)
    {
        var colIsOdd = ((int)MathF.Floor(col) & 1) != 0;
        var x = col * tileOffset.X;
        var y = row * tileOffset.Y * 2 + (colIsOdd ? tileOffset.Y : 0f);
        return new Vector2(x, y);
    }

    private static Vector2 OffsetToPixelAvg(float col, float row, Vector2 tileOffset)
    {
        var x = col * tileOffset.X;
        var y = row * tileOffset.Y * 2 + tileOffset.Y / 2f;
        return new Vector2(x, y);
    }

    public void Dispose() => _tileset.Dispose();
}