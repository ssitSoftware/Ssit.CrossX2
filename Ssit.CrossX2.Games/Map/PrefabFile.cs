using System.IO.Compression;
using System.Text;
using Ssit.CrossX2.Framework.Games.Template;

namespace Ssit.CrossX2.Framework.Games.Map;

/// <summary>
/// A piece of a map (tiles and objects) saved so it can be placed again. Every part is a layer of
/// <see cref="Width"/> x <see cref="Height"/> tiles; object positions are relative to the prefab's top-left corner.
/// </summary>
public class PrefabFile
{
    public const string Extension = ".prefab";

    private const string Header = "CROSSX2 PREFAB";
    private const int Version = 1;
    private const int GuidLength = 16;

    public Guid TemplateId { get; }
    public int TileSize { get; }
    public string[] Tilesets { get; }

    /// <summary>Size in tiles.</summary>
    public int Width { get; }

    /// <summary>Size in tiles.</summary>
    public int Height { get; }

    /// <summary>
    /// True when extracted from several layers: each part has to be placed on the layer with the part's
    /// <see cref="MapLayer.Id"/>. False for a single-layer prefab, which can be placed on any layer.
    /// </summary>
    public bool IsLayerBound { get; set; }

    /// <summary>PNG image of the prefab, for the prefab library.</summary>
    public byte[] PreviewPng { get; set; }

    /// <summary>The parts, in the map's layer (drawing) order.</summary>
    public List<MapLayer> Layers { get; } = new();

    public PrefabFile(Guid templateId, int tileSize, string[] tilesets, int width, int height)
    {
        TemplateId = templateId;
        TileSize = tileSize;
        Tilesets = tilesets;
        Width = width;
        Height = height;
    }

    public void Save(Stream stream)
    {
        using var gzipStream = new GZipStream(stream, CompressionLevel.Optimal, leaveOpen: true);
        using var writer = new BinaryWriter(gzipStream, Encoding.UTF8, leaveOpen: true);

        writer.Write(Encoding.ASCII.GetBytes(Header));
        writer.Write(Version);
        writer.Write(TemplateId.ToByteArray());
        writer.Write((byte)TileSize);

        writer.Write((byte)Tilesets.Length);
        for (var idx = 0; idx < Tilesets.Length; ++idx)
        {
            writer.Write(Tilesets[idx]);
        }

        writer.Write(Width);
        writer.Write(Height);
        writer.Write(IsLayerBound);

        // The preview goes before the layers, so ReadInfo can stop after it.
        var preview = PreviewPng ?? Array.Empty<byte>();
        writer.Write(preview.Length);
        writer.Write(preview);

        writer.Write((byte)Layers.Count);
        foreach (var layer in Layers)
        {
            layer.Save(writer);
        }

        writer.Flush();
    }

    /// <summary>Loads the whole prefab. Tile sets are remapped to <paramref name="tilesetsOverride"/>
    /// (the editor's tile set order) the same way <see cref="MapFile.FromStream"/> does it.</summary>
    public static PrefabFile FromStream(Stream stream, IGameTemplate gameTemplate, string[] tilesetsOverride = null)
    {
        using var gzipStream = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true);
        using var reader = new BinaryReader(gzipStream, Encoding.UTF8, leaveOpen: true);

        var prefab = ReadHeader(reader, tilesetsOverride, out var tilesets);

        var layersNo = reader.ReadByte();
        for (var idx = 0; idx < layersNo; ++idx)
        {
            var id = reader.ReadString();
            var layer = new MapLayer(id, gameTemplate);
            layer.Load(reader);
            prefab.Layers.Add(layer);
        }

        if (tilesetsOverride != null)
        {
            var mapping = new int[tilesets.Length];

            for (var idx = 0; idx < tilesets.Length; ++idx)
            {
                mapping[idx] = Array.IndexOf(tilesetsOverride, tilesets[idx]);
            }

            foreach (var layer in prefab.Layers)
            {
                layer.RemapTilesets(mapping);
            }
        }

        return prefab;
    }

    /// <summary>Reads size, layer binding and preview without the layers (for listing prefabs).</summary>
    public static PrefabFile ReadInfo(Stream stream)
    {
        using var gzipStream = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true);
        using var reader = new BinaryReader(gzipStream, Encoding.UTF8, leaveOpen: true);

        return ReadHeader(reader, null, out _);
    }

    private static PrefabFile ReadHeader(BinaryReader reader, string[] tilesetsOverride, out string[] tilesets)
    {
        var bytes = reader.ReadBytes(Header.Length);

        if (bytes.Length != Header.Length)
            throw new InvalidDataException("Not a prefab file!");

        for (var idx = 0; idx < bytes.Length; ++idx)
        {
            if (Header[idx] != bytes[idx]) throw new InvalidDataException("Not a prefab file!");
        }

        var version = reader.ReadInt32();
        if (version > Version)
            throw new InvalidDataException($"Prefab file version {version} is newer than supported ({Version}).");

        var templateId = new Guid(reader.ReadBytes(GuidLength));
        var tileSize = reader.ReadByte();

        var tilesetCount = reader.ReadByte();
        tilesets = new string[tilesetCount];

        for (var idx = 0; idx < tilesetCount; ++idx)
        {
            tilesets[idx] = reader.ReadString();
        }

        var width = reader.ReadInt32();
        var height = reader.ReadInt32();
        var isLayerBound = reader.ReadBoolean();

        var previewLength = reader.ReadInt32();
        var preview = previewLength > 0 ? reader.ReadBytes(previewLength) : null;

        return new PrefabFile(templateId, tileSize, tilesetsOverride ?? tilesets, width, height)
        {
            IsLayerBound = isLayerBound,
            PreviewPng = preview
        };
    }
}
