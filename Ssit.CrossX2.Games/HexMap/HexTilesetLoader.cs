using System.Numerics;
using Newtonsoft.Json;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Misc;
using Ssit.CrossX2.Framework.IO;
using Ssit.CrossX2.Framework.IoC;
using Ssit.CrossX2.Framework.Utils;

namespace Ssit.CrossX2.Framework.Games.HexMap;

internal static class HexTilesetLoader
{
    private class Json
    {
        [JsonProperty("hexOffset")]
        public JsonPoint HexOffset { get; set; }

        [JsonProperty("hexMiddle")]
        public int HexMiddle { get; set; }

        [JsonProperty("tiles")]
        public string[] Tiles { get; set; }
    }

    private class JsonPoint
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }
    }

    public static IDisposable Load(string path, IFilesProvider filesProvider, IIoCContainer container)
    {
        using var jsonStream = filesProvider.Open(path);
        var json = JsonConvert.DeserializeObject<Json>(new StreamReader(jsonStream).ReadToEnd());

        var dir = Path.GetDirectoryName(path) ?? "";
        var images = new RgbaColor[json.Tiles.Length][,];
        
        for (var idx = 0; idx < json.Tiles.Length; idx++)
        {
            var tilePath = Path.Combine(dir, json.Tiles[idx] + ".png");
            using var tileStream = filesProvider.Open(tilePath);
            images[idx] = ImagesUtility.LoadImage(tileStream);
        }

        var pack = AtlasPacker.Pack(images);
        var atlas = new RgbaColor[pack.Size.Width, pack.Size.Height];
        var tiles = new HexTile[images.Length];

        for (var idx = 0; idx < images.Length; idx++)
        {
            var image = images[idx];
            var imageWidth = image.GetLength(0);
            var imageHeight = image.GetLength(1);
            var (offsetX, offsetY) = pack.Positions[idx];

            for (var x = 0; x < imageWidth; x++)
            {
                for (var y = 0; y < imageHeight; y++)
                {
                    atlas[offsetX + x, offsetY + y] = image[x, y];
                }
            }

            var origin = new Vector2(imageWidth / 2f, imageHeight - json.HexMiddle);
            tiles[idx] = new HexTile(new Rectangle(offsetX, offsetY, imageWidth, imageHeight), origin);
        }

        using var atlasStream = ImagesUtility.GetStream(atlas);
        var texture = container.IoCConstruct<ITexture>(new LoadTextureParameters
        {
            DiffuseMapStream = atlasStream
        });

        var tileOffset = new Size(json.HexOffset.X, json.HexOffset.Y);
        return new HexTileset(texture, tiles, tileOffset);
    }
}
