namespace Ssit.CrossX2.IO;

public interface IAssetsSource
{
    IFilesProvider FilesProvider { get; }
    string DriveName { get; }
    string DefinitionPath => DriveName + "/Fonts.json";
}