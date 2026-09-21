using System.Collections.Concurrent;
using SDL;
using static SDL.SDL3_mixer;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Audio;

internal unsafe class SdlTrackPool(SdlSoundManagerImpl soundManager)
{
    private readonly ConcurrentQueue<SdlHandle<MIX_Track>> _tracks = new();

    public SdlHandle<MIX_Track> GetAvailableTrack()
    {
        if (_tracks.TryDequeue(out var track))
        {
            return track;
        }

        MIX_Track* trackPtr = MIX_CreateTrack(soundManager.MixerHandle.Pointer);

        if (trackPtr != null)
        {
            return new SdlHandle<MIX_Track>(trackPtr);
        }

        return SdlHandle<MIX_Track>.Empty;
    }
    
    public void ReturnTrack(SdlHandle<MIX_Track> track)
    {
        _tracks.Enqueue(track);
    }
}