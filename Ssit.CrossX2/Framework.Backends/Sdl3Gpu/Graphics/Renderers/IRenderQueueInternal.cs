using Ssit.CrossX2.Framework.Graphics.Renderers;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics.Renderers;

internal interface IRenderQueueInternal : IRenderQueue
{
    void Flush();
}