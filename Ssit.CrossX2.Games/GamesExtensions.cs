using Ssit.CrossX2.Framework.Content;
using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.Games;

public static class GamesExtensions
{
    public static void RegisterGameContentTypes(this IContentManager contentManager, ContentAlign alignment)
    {
        contentManager.RegisterLoader<GameObject>(path => new GameObject(path, contentManager, alignment));
    }
}