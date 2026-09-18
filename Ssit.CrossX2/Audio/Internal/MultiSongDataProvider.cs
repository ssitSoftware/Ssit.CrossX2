using Ssit.CrossX2.IO;

namespace Ssit.CrossX2.Audio.Internal;

// This class was partially created with Claude Code assistance
public class MultiSongDataProvider : IMusicDataProvider
{
    private readonly IFilesProvider _filesProvider;
    private readonly IReadOnlyList<Song> _songs;
    private int _songIndex;
    private VorbisDataProvider _current;
    private short[] _tempBuffer;

    public int Position { get; private set; }
    public int Frequency { get; private set; } = 0;

    public int CurrentSongIndex => _songIndex;
    public int CurrentSongBlock => _current?.Position ?? 0;

    public MultiSongDataProvider(IFilesProvider filesProvider, IReadOnlyList<Song> songs,
        int startSongIndex = 0, int startBlockOffset = 0, int blockSize = 88200 / 10)
    {
        _filesProvider = filesProvider;
        _songs = songs;
        
        OpenSong(startSongIndex);
        
        if (startBlockOffset > 0)
        {
            _current.Skip(startBlockOffset, blockSize);
        }
    }

    private void OpenSong(int index)
    {
        _current?.Dispose();
        
        var stream = _filesProvider.Open(_songs[index].Path);
        
        _current = new VorbisDataProvider(stream);
        
        _songIndex = index;

        if (Frequency == 0 || index == 0)
        {
            Frequency = _current.Frequency;
        }
    }

    public int Read(short[] buffer)
    {
        if(_current is null) return 0;
        
        var samplesRead = _current.Read(buffer);

        if (samplesRead < buffer.Length)
        {
            OpenSong((_songIndex + 1) % _songs.Count);

            if (samplesRead < buffer.Length)
            {
                if (_tempBuffer == null || _tempBuffer.Length < buffer.Length)
                {
                    _tempBuffer = new short[buffer.Length];
                }

                var more = _current.Read(_tempBuffer);
                var toCopy = Math.Min(more, buffer.Length - samplesRead);
                
                Array.Copy(_tempBuffer, 0, buffer, samplesRead, toCopy);
                samplesRead += toCopy;
            }
        }

        if (samplesRead > 0) Position++;
        
        return samplesRead;
    }

    public void Dispose()
    {
        _current?.Dispose();
        _current = null;
    }
}
