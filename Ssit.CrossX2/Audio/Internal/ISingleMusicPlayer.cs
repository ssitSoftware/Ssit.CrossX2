namespace Ssit.CrossX2.Audio.Internal;

public interface ISingleMusicPlayer: IDisposable
{
    void Start(IMusicDataProvider provider, int bufferLength, int fadeInMilliseconds);
    void Update(float deltaTime, out bool finished);
    void FadeOut(int fadeOutMilliseconds);
    int Position { get; }
    string Name { get; set; }
}