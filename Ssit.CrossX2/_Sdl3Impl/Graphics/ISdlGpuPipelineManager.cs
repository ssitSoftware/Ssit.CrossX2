namespace Ssit.CrossX2._Sdl3Impl.Graphics;

internal interface ISdlGpuPipelineManager: IDisposable
{
    ISdlGpuPipeline GetProperPipeline(bool texturedRendering);
}