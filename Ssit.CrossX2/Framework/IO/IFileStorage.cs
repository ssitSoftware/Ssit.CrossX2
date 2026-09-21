namespace Ssit.CrossX2.Framework.IO;

public interface IFileStorage
{
    string ReadText(string path);
    void WriteText(string path, string text);

    void WriteData(string path, byte[] data);
    byte[] ReadData(string path);
}