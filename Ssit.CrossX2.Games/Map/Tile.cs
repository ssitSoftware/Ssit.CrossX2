namespace Ssit.CrossX2.Framework.Games.Map;

public readonly struct Tile
{
    public readonly uint Value;

    public Tile(int tileSet, int x, int y, int material) => Value = (uint) ((tileSet & 0xff) << 24 | (material & 0xff) << 16 | (y & 0xff) << 8 | (x & 0xff));
    
    public Tile(uint value) => Value = value;

    public Tile ConvertFromOld()
    {
        if (IsEmpty)
            return Empty;
        
        var tileSet = (int)((Value >> 24) & 0xff);
        
        var y = (int)((Value >> 12) & 0xfff) & 0xff;
        var x = (int)(Value & 0xfff) & 0xff;

        return new(tileSet, x, y, 0);
    }
    
    public Tile() => Value = uint.MaxValue;

    public int TileSet => (int)((Value >> 24) & 0xff);
    
    public int X => (int)(Value & 0xff);
    public int Y => (int)((Value >> 8) & 0xff);
    
    public int Material => (int)((Value >> 16) & 0xff);

    public bool IsEmpty => Value == uint.MaxValue;
    
    public static readonly Tile Empty = new ();
}