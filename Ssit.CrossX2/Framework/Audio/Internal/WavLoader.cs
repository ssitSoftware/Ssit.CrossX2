using System.Text;

namespace Ssit.CrossX2.Framework.Audio.Internal;

public static class WavLoader
{
    public static (short[] buffer, int sampleRate) LoadMonoWav(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);

        // RIFF header
        reader.ReadBytes(4); // "RIFF"
        reader.ReadInt32();  // file size
        reader.ReadBytes(4); // "WAVE"

        var channels = 1;
        var sampleRate = 44100;
        var bitsPerSample = 16;
        short[] audioData = null;

        while (stream.Position < stream.Length - 8)
        {
            var chunkIdBytes = reader.ReadBytes(4);
            var chunkSize = reader.ReadInt32();
            var chunkEnd = stream.Position + chunkSize;

            var id = Encoding.ASCII.GetString(chunkIdBytes);

            if (id == "fmt ")
            {
                reader.ReadInt16(); // audio format (1 = PCM)
                channels = reader.ReadInt16();
                sampleRate = reader.ReadInt32();
                reader.ReadInt32(); // byte rate
                reader.ReadInt16(); // block align
                bitsPerSample = reader.ReadInt16();
            }
            else if (id == "data")
            {
                audioData = ReadMonoData(reader, chunkSize, channels, bitsPerSample);
            }

            stream.Position = chunkEnd;
        }

        return (audioData ?? Array.Empty<short>(), sampleRate);
    }

    private static short[] ReadMonoData(BinaryReader reader, int dataSize, int channels, int bitsPerSample)
    {
        var bytesPerSample = bitsPerSample / 8;
        var totalFrames = dataSize / (bytesPerSample * channels);
        var result = new short[totalFrames];

        for (var i = 0; i < totalFrames; i++)
        {
            var sum = 0;
            for (var c = 0; c < channels; c++)
            {
                if (bitsPerSample == 16)
                {
                    sum += reader.ReadInt16();
                }
                else if (bitsPerSample == 8)
                {
                    sum += (reader.ReadByte() - 128) << 8;
                }
                else if (bitsPerSample == 24)
                {
                    reader.ReadByte(); // lo
                    int mid = reader.ReadByte();
                    int hi = (sbyte)reader.ReadByte();
                    sum += (hi << 8) | mid ; // keep top 16 bits
                }
            }
            result[i] = (short)(sum / channels);
        }

        return result;
    }
}
