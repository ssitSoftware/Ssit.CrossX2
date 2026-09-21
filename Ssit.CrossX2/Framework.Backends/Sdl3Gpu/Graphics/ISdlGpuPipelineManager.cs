using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics;

internal interface ISdlGpuPipelineManager: IDisposable
{
    ISdlGpuPipeline GetProperPipeline(bool texturedRendering, PrimitiveType primitiveType);
}