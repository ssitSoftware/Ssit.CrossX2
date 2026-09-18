namespace Ssit.CrossX2.Audio.Internal;

public interface IMusicDataProvider : IDisposable
{
    int Position { get; }
    int Frequency { get; }
    int Read(short[] buffer);
}
