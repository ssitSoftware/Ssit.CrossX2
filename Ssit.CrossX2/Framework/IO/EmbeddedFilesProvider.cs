using System.Reflection;

namespace Ssit.CrossX2.Framework.IO;

public class EmbeddedFilesProvider: IFilesProvider
{
    private Assembly _assembly;
    private HashSet<string> _files;
    private string _prefix;

    public EmbeddedFilesProvider(Assembly assembly, string prefix = null)
    {
        _prefix = (prefix ?? assembly.GetName().Name) + '.';
        _assembly = assembly;
        _files = new HashSet<string>(_assembly.GetManifestResourceNames());
    }
    
    private string GetResourceName(string path)
    {
        path = PathHelper.NormalizePath(path).Replace('/', '.');
        path = _prefix + path;
        return path;
    }
    
    public Stream Open(string path)
    {
        path = GetResourceName(path).Replace("!", "");

        if (!_files.Contains(path))
        {
            throw new FileNotFoundException($"Resource {path} not found in assembly {_assembly.GetName().Name}.");
        }
        
        var stream = _assembly.GetManifestResourceStream(path);
        return stream ?? throw new InvalidProgramException();
    }

    public bool FileExists(string path)
    {
        path = GetResourceName(path).Replace("!", "");
        return _files.Contains(path);
    }

    public string[] GetFiles(string path, string extension = null)
    {
        path = GetResourceName(path);
        return _files.Where(o => o.StartsWith(path) && (extension?.Equals(o.Split('.')[^1]) ?? true)).Select( o=>o.Substring(_prefix.Length)).ToArray();
    }

    public string GetPhisicalFilePath(string path)
    {
        throw new NotSupportedException();
    }
}