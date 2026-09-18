using Ssit.CrossX2.Template;

namespace Ssit.CrossX2.Map;

public class MainLayer : MapLayer
{
    public override string Id
    {
        get => LayerDescription.MainLayerId;
        set
        {
            if (value != LayerDescription.MainLayerId)
                throw new Exception($"Layer id can't be changed. Current value is {LayerDescription.MainLayerId}");
        }
    }

    public override string Name
    {
        get => LayerDescription.MainLayerName;
        set { }
    }

    public Size[,] CameraAreas { get; private set; } = new Size[0, 0];
    
    public MainLayer(IGameTemplate gameTemplate) : base(LayerDescription.MainLayerId, 1, 1, gameTemplate)
    {
    }

    public MainLayer(int width, int height, IGameTemplate gameTemplate) : base(LayerDescription.MainLayerId, width, height, gameTemplate)
    {
    }

    protected override void LoadInternal(BinaryReader reader)
    {
        base.LoadInternal(reader);
        var width = reader.ReadUInt16();
        var height = reader.ReadUInt16();
        
        CameraAreas = new Size[width, height];
        
        var sizes = new uint[width * height];
        var bufferSize = sizes.Length * sizeof(uint);
        
        var bytes = reader.ReadBytes(bufferSize);
        Buffer.BlockCopy(bytes, 0, sizes, 0, bufferSize);
        
        for (var x = 0; x < width; ++x)
        {
            for (var y = 0; y < height; ++y)
            {
                var idx = y * width + x;
                CameraAreas[x, y] = new Size((int)(sizes[idx] & 0xffff), (int)(sizes[idx] >> 16));
            }
        }
    }

    protected override void SaveInternal(BinaryWriter writer)
    {
        base.SaveInternal(writer);

        var width = (ushort)CameraAreas.GetLength(0);
        var height = (ushort)CameraAreas.GetLength(1);
        
        writer.Write(width);
        writer.Write(height);
        
        var sizes = new uint[width * height];
        var bytes =  new byte[sizes.Length * sizeof(uint)];
        
        for (var x = 0; x < width; ++x)
        {
            for (var y = 0; y < height; ++y)
            {
                var idx = y * width + x;
                var val = CameraAreas[x, y];
                sizes[idx] = (uint)((val.Width & 0xffff) | ((val.Height & 0xffff) << 16));
            }
        }

        if (bytes.Length > 0)
        {
            Buffer.BlockCopy(sizes, 0, bytes, 0, bytes.Length);
            writer.Write(bytes);
        }
    }

    public override void Resize(int width, int height, MapAlign align = MapAlign.Left)
    {
        base.Resize(width, height, align);
        UpdateCameraAreas();
    }

    private void UpdateCameraAreas()
    {
        if (CameraAreas.GetLength(0) == 0)
            return;
    }
}