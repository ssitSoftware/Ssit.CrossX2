using System.Numerics;
using Ssit.CrossX2.Framework.Games.Editor;
using Ssit.CrossX2.Framework.Games.Template;
using Ssit.CrossX2.Framework.Utils;

namespace Ssit.CrossX2.Framework.Games.Map;

public class MapLayer: BindableModel
{
    public class LayerNameHandler: IPropertyHandler
    {
        public bool Validate(object value)
        {
            var str = value?.ToString() ?? "";
            return !string.IsNullOrWhiteSpace(str) && str.ToLowerInvariant() != "main";
        }

        public bool Enable(object value)
        {
            var str = value?.ToString() ?? "";
            return str.ToLowerInvariant() != "main";
        }
    }
    
    private string _name;
    private float _depth;
    private float _horizontalSpeed = 1;
    private float _verticalSpeed = 1;
    private RgbaColor _tintColor = RgbaColor.White;
    private RgbaColor _fogColor = RgbaColor.Transparent;
    private bool _enableLighting;
    private string _id;

    public virtual string Id
    {
        get => _id;
        set => _id = value;
    }

    internal IGameTemplate GameTemplate { get; }
    
    [Editor(typeof(LayerNameHandler))]
    public virtual string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }
    
    [EditorLayerSize]
    public Size Size 
    { 
        get => new(Width, Height);
        set => throw new NotSupportedException();
    }

    [EditorFloat(-100, 100, 0.125f)]
    public float Depth
    {
        get => _depth;
        set => SetField(ref _depth, value);
    }
    
    [EditorFloat(0, 2, 0.125f)]
    public float HorizontalSpeed
    {
        get => _horizontalSpeed;
        set => SetField(ref _horizontalSpeed, value);
    }

    [EditorFloat(0, 2, 0.125f)]
    public float VerticalSpeed
    {
        get => _verticalSpeed;
        set => SetField(ref _verticalSpeed, value);
    }

    [System.ComponentModel.Editor]
    public RgbaColor TintColor
    {
        get => _tintColor;
        set => SetField(ref _tintColor, value);
    }

    [System.ComponentModel.Editor]
    public RgbaColor FogColor
    {
        get => _fogColor;
        set => SetField(ref _fogColor, value);
    }

    [System.ComponentModel.Editor]
    public bool EnableLighting
    {
        get => _enableLighting;
        set => SetField(ref _enableLighting, value);
    }

    public Tile[,] Tiles { get; private set; } = new Tile[0, 0];

    public List<MapObject> Objects { get; } = new();
    
    public int Width => Tiles.GetLength(0);
    public int Height => Tiles.GetLength(1);

    internal MapLayer(string id, IGameTemplate gameTemplate)
    {
        _id = id;
        GameTemplate = gameTemplate;
    }

    public MapLayer(string id, int width, int height, IGameTemplate gameTemplate) : this(id, gameTemplate)
    {
        ResizeInternal(width, height);
    }

    public virtual void Resize(int width, int height, MapAlign align = MapAlign.Left)
    {
        ResizeInternal(width, height, align);
    }
    
    protected void ResizeInternal(int width, int height, MapAlign align = MapAlign.Left)
    {
        var oldWidth = Width;
        var oldHeight = Height;

        var offsetX = 0;
        var offsetY = 0;

        switch (align & MapAlign.Horizontal)
        {
            case MapAlign.Center:
                offsetX = (width - oldWidth) / 2;
                break;
            
            case MapAlign.Right:
                offsetX = width - oldWidth;
                break;
        }
        
        switch (align & MapAlign.Vertical)
        {
            case MapAlign.VCenter:
                offsetY = (height - oldHeight) / 2;
                break;
            
            case MapAlign.Bottom:
                offsetY = height - oldHeight;
                break;
        }

        var tiles = new Tile[width, height];

        for (var ix = 0; ix < width; ++ix)
        {
            var oldX = ix - offsetX;

            for (var iy = 0; iy < height; ++iy)
            {
                var oldY = iy - offsetY;

                if (oldX < 0 || oldX >= oldWidth || oldY < 0 || oldY >= oldHeight)
                {
                    tiles[ix, iy] = Tile.Empty;
                }
                else
                {
                    tiles[ix, iy] = Tiles[oldX, oldY];
                }
            }
        }

        Tiles = tiles;

        foreach (var obj in Objects)
        {
            obj.Position += new Vector2(offsetX, offsetY);
        }
        
        OnPropertyChanged(nameof(Width));
        OnPropertyChanged(nameof(Height));
    }

    public void RemapTilesets(int[] mapping)
    {
        for (var ix = 0; ix < Width; ++ix)
        {
            for (var iy = 0; iy < Height; ++iy)
            {
                var tile = Tiles[ix, iy];
                if (tile.IsEmpty) continue;

                var newValue = mapping[tile.TileSet];
                if (newValue < 0)
                {
                    Tiles[ix, iy] = Tile.Empty;
                }
                else if ( newValue != tile.TileSet)
                {
                    Tiles[ix, iy] = new(newValue, tile.X, tile.Y, tile.Material);
                }
            }
        }
    }
    
     internal void Load(BinaryReader reader) => LoadInternal(reader);
     internal void Save(BinaryWriter writer) => SaveInternal(writer);

     protected virtual void LoadInternal(BinaryReader reader)
     {
        var gameTemplate = GameTemplate;
        Name = reader.ReadString();
        Depth = reader.ReadSingle();
        HorizontalSpeed = reader.ReadSingle();
        VerticalSpeed = reader.ReadSingle();
        reader.ReadBoolean(); // old
        reader.ReadBoolean(); // old

        var width = reader.ReadInt32();
        var height = reader.ReadInt32();

        FogColor = RgbaColor.FromRgba(reader.ReadUInt32(), false);
        TintColor = RgbaColor.FromRgba(reader.ReadUInt32(), false);
        
        EnableLighting = reader.ReadBoolean();
        
        Resize(width, height);

        var bufferSize = width * height * sizeof(uint);
        var bytes = reader.ReadBytes(bufferSize);

        var tiles = new uint[width * height];
        
        Buffer.BlockCopy(bytes, 0, tiles, 0, bufferSize);

        for (var x = 0; x < width; ++x)
        {
            for (var y = 0; y < height; ++y)
            {
                var idx = y * width + x;
                Tiles[x, y] = new(tiles[idx]);
            }
        }

        var objects = reader.ReadInt32();
        Objects.Clear();
        
        for (var idx = 0; idx < objects; ++idx)
        {
            Objects.Add(MapObject.Load(reader, gameTemplate));
        }
        SortObjects();
    }

    protected virtual void SaveInternal(BinaryWriter writer)
    {
        writer.Write(Id);
        writer.Write(Name);
        writer.Write(Depth);
        writer.Write(HorizontalSpeed);
        writer.Write(VerticalSpeed);
        
        writer.Write(false);
        writer.Write(false);
        
        writer.Write(Width);
        writer.Write(Height);
        
        writer.Write(FogColor.ToUInt32());
        writer.Write(TintColor.ToUInt32());
        
        writer.Write(EnableLighting);
        
        var bufferSize = Width * Height * sizeof(uint);
        var bytes = new byte[bufferSize];
        var tiles = new uint[Width * Height];

        var width = Width;
        var height = Height;
        
        for (var x = 0; x < width; ++x)
        {
            for (var y = 0; y < height; ++y)
            {
                var idx = y * width + x;
                tiles[idx] = Tiles[x, y].Value;
            }
        }
        
        Buffer.BlockCopy(tiles, 0, bytes, 0, bufferSize);
        writer.Write(bytes);
        
        writer.Write(Objects.Count);
        foreach (var obj in Objects)
        {
            obj.Save(writer);
        }
    }

    public MapObject FindObject(int id)
    {
        for (var idx = 0; idx < Objects.Count; ++idx)
        {
            if (Objects[idx].Id == id)
            {
                return Objects[idx];
            }
        }

        return null;
    }

    public void SortObjects()
    {
        Objects.Sort((a, b) => a.ZOrder.CompareTo(b.ZOrder));
    }
}