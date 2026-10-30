using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Games.HexMap;
using Ssit.CrossX2.Framework.Games.Services;
using Ssit.CrossX2.Framework.Games.UI;
using Ssit.CrossX2.Framework.IoC;
using Ssit.CrossX2.Framework.UI;

namespace Ssit.CrossX2.Framework.Games;

public static class ServiceRegistrar
{
    public static IUiAppBuilder WithXxGames(this IUiAppBuilder builder)
    {
        builder
            .WithServiceRegistrar(s => s.WithSingleton<IGameDebugService, GameDebugService>())
            .WithHandlersMapping(m => m.AddMapping<GameView, GameViewHandler>());
        
        return builder;
    }

    public static IIoCContainerBuilder WithHexTileset(this IIoCContainerBuilder builder)
    {
        builder.WithPostBuildDelegate<IContentManager>((cm, container) =>
            cm.RegisterLoader<HexTileset>(path => HexTilesetLoader.Load(path, cm.FilesProvider, container)));
        return builder;
    }
}