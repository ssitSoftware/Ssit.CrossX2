namespace Ssit.CrossX2.Framework.Games.Map;

/// <summary>
/// Helpers for map paths.
/// </summary>
public static class MapPathHelper
{
    /// <summary>
    /// Id of the map used to name its files in the application storage (map cache, map state in checkpoints):
    /// the map path without <paramref name="mapPathPrefix"/> (if it starts with it) and without the file extension,
    /// with directory separators and the drive colon replaced by underscores.
    /// E.g. prefix "assets:/Game/Maps/", path "assets:/Game/Maps/Map01.map" → "Map01".
    /// </summary>
    public static string MapId(string mapPath, string mapPathPrefix = null)
    {
        var id = mapPath;

        if (!string.IsNullOrEmpty(mapPathPrefix) && id.StartsWith(mapPathPrefix, StringComparison.Ordinal))
        {
            id = id[mapPathPrefix.Length..];
        }

        // Extension of the file name only - a dot in a directory name is kept.
        var nameStart = id.LastIndexOfAny(['/', '\\', ':']) + 1;
        var extension = id.LastIndexOf('.');
        if (extension > nameStart)
        {
            id = id[..extension];
        }

        return id.Replace('\\', '_').Replace('/', '_').Replace(':', '_');
    }
}
