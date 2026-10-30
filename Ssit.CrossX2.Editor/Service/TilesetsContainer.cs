using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using Ssit.CrossX2.Editor.Helpers;
using Ssit.CrossX2.Framework.Games;
using Ssit.CrossX2.Framework.Games.Template;
using Ssit.CrossX2.Framework.Utils;

namespace Ssit.CrossX2.Editor.Service
{
    public class TilesetsContainer: ITilesetsContainer
    {
        private readonly IGameTemplate _gameTemplate;

        public Tileset[] TileSets { get; private set; }

        public TilesetsContainer(IGameTemplate gameTemplate)
        {
            _gameTemplate = gameTemplate;
        }
    
        public void Load()
        {
            var list = new List<Tileset>();
        
            var paths = _gameTemplate.TileSets.ToArray();

            foreach (var path in paths)
            {
                using var stream = _gameTemplate.AssetsProvider.Open(path);
                var image = SKImage.FromEncodedData(stream);

                var cutPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path)!,
                    System.IO.Path.GetFileNameWithoutExtension(path));

                TilesetMeta meta = null;
                var tilesPath = cutPath + ".tiles";
                if (_gameTemplate.AssetsProvider.FileExists(tilesPath))
                {
                    using var metaStream = _gameTemplate.AssetsProvider.Open(tilesPath);
                    meta = TilesetMeta.FromStream(metaStream);
                }

                SKImage mask = null;
                var maskPath = cutPath + ".mask.png";
                if (_gameTemplate.AssetsProvider.FileExists(maskPath))
                {
                    using var maskStream = _gameTemplate.AssetsProvider.Open(maskPath);
                    using var maskBitmap = ImagesUtility.LoadMask(maskStream);
                    mask = SKImage.FromBitmap(maskBitmap);
                }

                list.Add(new Tileset(image, meta, mask, path));
            }

            TileSets = list.ToArray();
        }
    }
}