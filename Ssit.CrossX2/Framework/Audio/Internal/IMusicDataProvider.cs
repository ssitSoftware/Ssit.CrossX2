namespace Ssit.CrossX2.Framework.Audio.Internal;

public interface IMusicDataProvider : IDisposable
{
    int Position { get; }
    int Frequency { get; }
    int Read(short[] buffer);
}
