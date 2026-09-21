using System.Numerics;
using Ssit.CrossX2.Framework.Games.Map;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.IoC;

namespace Ssit.CrossX2.Framework.Games.Rendering.Map;

public class TilesDisplaySegmentBuilder
{
    private const int MergeSize = 32;
    
    private readonly Dictionary<int, List<Quad>> _verticesMap = new();
    private readonly Tile?[,] _temp = new Tile?[MergeSize, MergeSize];
    
    private string[] _tileSets;
    private int _startX;
    private int _startY;
    
    private int _endX;
    private int _endY;

    private int _tileSize;
    
    private Tile[,] _tiles;
    
    private IIoCContainer _container;

    public TilesDisplaySegmentBuilder WithServices(IIoCContainer container)
    {
        _container = container;
        return this;
    }
    
    public TilesDisplaySegmentBuilder WithMap(MapFile mapFile)
    {
        _tileSets = mapFile.Tilesets;
        return this;
    }
    
    public TilesDisplaySegmentBuilder WithLayer(MapLayer layer)
    {
        _tiles = layer.Tiles;
        return this;
    }

    public TilesDisplaySegmentBuilder WithTileSize(int tileSize)
    {
        _tileSize = tileSize;
        return this;
    }

    public TilesDisplaySegmentBuilder WithBounds(Rectangle bounds)
    {
        _startX = bounds.X;
        _startY = bounds.Y;
        _endX = bounds.Right;
        _endY = bounds.Bottom;
        
        return this;
    }

    public TilesDisplaySegment[] Build()
    {
        var list = new List<TilesDisplaySegment>();
        
        
        
        try
        {
            _verticesMap.Clear();

            for (var xx = _startX; xx < _endX; xx++)
            {
                for (var yy = _startY; yy < _endY; yy++)
                {
                    var tile = GetTile(_tiles, xx, yy, out var w, out var h);
                    if (tile.IsEmpty)
                    {
                        continue;
                    }

                    var tl = new Vector2(xx * _tileSize, yy * _tileSize);
                    var br = tl + new Vector2(_tileSize * w, _tileSize * h);

                    var xox = (float)_tileSize;
                    var xoy = (float)_tileSize;

                    var xtl = new Vector2(tile.X * xox, tile.Y * xoy);
                    var xbr = xtl + new Vector2(xox * w, xoy * h);

                    if (!_verticesMap.TryGetValue(tile.TileSet, out List<Quad> quads))
                    {
                        quads = new List<Quad>();
                        _verticesMap.Add(tile.TileSet, quads);
                    }

                    float epsilon = 0.0001f;
                    
                    xtl.X += epsilon;
                    xtl.Y += epsilon;
                    
                    xbr.X -= epsilon;
                    xbr.Y -= epsilon;
                    
                    quads.Add(new Quad(new RectangleF(tl, br - tl), new RectangleF(xtl, xbr - xtl)));
                }
            }

            foreach (var (key, quads) in _verticesMap)
            {
                var texture = _tileSets[key];

                list.Add(_container.IoCConstruct<TilesDisplaySegment>(new TilesDisplaySegment.Parameters
                {
                    Quads = quads.ToArray(),
                    TexturePath = texture
                }));
            }
            return list.ToArray();
        }
        finally
        {
            _verticesMap.Clear();
        }
    }

    private Tile GetTile(Tile[,] tiles, int xx, int yy, out int width, out int height)
    {
        var maxX = Math.Min(xx + MergeSize, _endX);
        var maxY = Math.Min(yy + MergeSize, _endY);
        
        var tile = tiles[xx, yy];
        width = 0;
        height = 0;
        
        if (tile.IsEmpty) return tile;
        
        var tileset = tile.TileSet;

        for (var idx = 0; idx < _temp.GetLength(0); ++idx)
        {
            for (var idy = 0; idy < _temp.GetLength(1); ++idy)
            {
                _temp[idx, idy] = null;
            }
        }
        
        for(var idx = xx; idx < maxX; ++idx)
        {
            for (var idy = yy; idy < maxY; ++idy)
            {
                var tile2 = tiles[idx, idy];
                
                if (tile2.IsEmpty || tile2.TileSet != tileset)
                {
                    _temp[idx - xx, idy - yy] = null;
                    continue;
                }

                if (tile2.X != tile.X + idx - xx || tile2.Y != tile.Y + idy - yy)
                {
                    _temp[idx - xx, idy - yy] = null;
                    continue;
                }
                
                _temp[idx-xx, idy-yy] = tile2;
            }
        }

        width = 1;
        height = 1;

        for (var idx = 0; idx < MergeSize; ++idx)
        {
            int left = 2;
            if (CheckFilled(width+1, height))
            {
                width++;
                left--;
            }
            
            if (CheckFilled(width, height+1))
            {
                height++;
                left--;
            }

            if (left == 2)
                break;
        }

        return tile;
    }

    private bool CheckFilled(int width, int height)
    {
        if (width > MergeSize || height > MergeSize)
            return false;
        
        for(var idx =0; idx < width; ++idx)
        {
            for (var idy = 0; idy < height; ++idy)
            {
                if (!_temp[idx, idy].HasValue)
                    return false;
            }
        }

        return true;
    }
}