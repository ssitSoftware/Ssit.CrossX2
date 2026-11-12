namespace Ssit.CrossX2.Framework.IO;

/// <summary>
/// Describes a named assets source and its backing files provider.
/// </summary>
public interface IAssetsSource
{
    IFilesProvider FilesProvider { get; }
    string DriveName { get; }
    string DefinitionPath => DriveName + "/Fonts.json";
}
