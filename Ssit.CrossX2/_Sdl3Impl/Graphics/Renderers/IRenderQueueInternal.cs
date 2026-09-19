using Ssit.CrossX2.Graphics.Renderers;

namespace Ssit.CrossX2._Sdl3Impl.Graphics.Renderers;

internal interface IRenderQueueInternal : IRenderQueue
{
    void Flush();
}