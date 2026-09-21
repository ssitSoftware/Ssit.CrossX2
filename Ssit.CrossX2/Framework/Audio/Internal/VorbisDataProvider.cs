using NVorbis;

namespace Ssit.CrossX2.Framework.Audio.Internal;

public class VorbisDataProvider : IMusicDataProvider
{
    private VorbisReader _reader;

    private float[] _buffer = new float[1];
    
    public int Position { get; private set; }
    public int Frequency => _reader.SampleRate;

    public VorbisDataProvider(Stream stream)
    {
        _reader = new VorbisReader(stream);
    }

    public void Skip(int blocks, int blockSize) 
    {
        if (_buffer.Length < blockSize)
        {
            _buffer = new float[blockSize];
        }

        for (var idx = 0; idx < blocks; ++idx)
        {
            if (_reader.ReadSamples(_buffer, 0, _buffer.Length) == 0)
            {
                return;
            }
            Position++;
        }
    }
    
    public int Read(short[] buffer)
    {
        if (_buffer.Length < buffer.Length)
        {
            _buffer = new float[buffer.Length];
        }

        if (_reader.IsEndOfStream)
        {
            return 0;
        }
        
        var samplesRead = _reader.ReadSamples(_buffer, 0, _buffer.Length);
        
        for (var idx = 0; idx < samplesRead; idx++)
        {
            buffer[idx] = (short)(_buffer[idx] * short.MaxValue);
        }

        for (var idx = samplesRead; idx < buffer.Length; idx++)
        {
            buffer[idx] = 0;
        }

        Position++;
        return samplesRead;
    }

    public void Dispose()
    {
        _reader?.Dispose();
        _reader = null;
    }
}