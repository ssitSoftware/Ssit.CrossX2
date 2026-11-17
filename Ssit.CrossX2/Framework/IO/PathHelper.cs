namespace Ssit.CrossX2.Framework.IO;

/// <summary>
/// Utility methods for normalized virtual file paths.
/// </summary>
public static class PathHelper
{
    public static string NormalizePath(string path)
    {
        path = path.Replace('\\', '/').Replace("//", "/");
        
        var parts = path.Split('/');

        var newPath = "";
        var skipNext = 0;
        
        for (var idx = parts.Length - 1; idx >= 0; idx--)
        {
            if (parts[idx] == "..")
            {
                skipNext++;
            }
            else
            {
                if (skipNext > 0)
                {
                    skipNext--;
                }
                else
                {
                    newPath = parts[idx] + '/' + newPath;
                }
            }
        }
        
        newPath = newPath.TrimEnd('/');
        return newPath;
    }

    public static void GetDriveAndPath(string path, out string drive, out string newPath)
    {
        path = NormalizePath(path);
        
        var parts = path.Split('/');
        newPath = string.Join('/', parts.Skip(1));
        drive = parts[0];
    }

    public static string GetPathWithExtension(string path, string extension)
    {
        var dir = Path.GetDirectoryName(path);
        var name = Path.GetFileNameWithoutExtension(path);
        
        return Path.Combine(dir!, $"{name}.{extension}");
    }

    public static string GetReadableFromPath(string path)
    {
        var text = Path.GetFileNameWithoutExtension(path);
        
        var startIndex = 0;
        var output = "";
        
        for (var idx = 1; idx < text.Length; ++idx)
        {
            var ch = text[idx];
            if (Char.IsUpper(ch))
            {
                if (output.Length > 0)
                {
                    output += " ";
                }
                output += text.Substring(startIndex, idx - startIndex);
                startIndex = idx;
            }
        }
        
        if (output.Length > 0)
        {
            output += " ";
        }
        output += text.Substring(startIndex, text.Length - startIndex);

        return output;
    }
}
