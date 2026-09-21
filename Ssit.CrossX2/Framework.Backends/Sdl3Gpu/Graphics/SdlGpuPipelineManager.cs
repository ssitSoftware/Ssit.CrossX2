using SDL;
using Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics.Pipelines;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.IoC;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics;

internal class SdlGpuPipelineManager(IIoCContainer ioCContainer, IRenderer renderer): ISdlGpuPipelineManager
{
    private const byte Lighting = 1;
    private const byte Texture = 2;
    private const byte Lines = 4;
    private const byte BumpMapping = 8;

    private readonly Dictionary<byte, ISdlGpuPipeline> _pipelines = new();

    public ISdlGpuPipeline GetProperPipeline(bool texturedRendering, PrimitiveType primitiveType)
    {
        var manager = (LightingManager)renderer.LightingManager;

        byte type = 0;

        if (texturedRendering) type |= Texture;
        if (manager.LightingEnabled) type |= Lighting;
        if (primitiveType == PrimitiveType.Lines) type |= Lines;
        if (primitiveType == PrimitiveType.TrianglesWithTangents && manager.LightingEnabled) type |= BumpMapping;

        if (_pipelines.TryGetValue(type, out var pipeline)) return pipeline;

        pipeline = CreatePipeline(type);
        _pipelines[type] = pipeline;

        return pipeline;
    }

    private ISdlGpuPipeline CreatePipeline(byte type)
    {
        switch (type)
        {
            case 0:
                return ioCContainer.IoCConstruct<SdlGpuColorPipeline>(SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST);

            case Lines:
                return ioCContainer.IoCConstruct<SdlGpuColorPipeline>(SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_LINELIST);

            case Lighting:
                return ioCContainer.IoCConstruct<ColorLightingPipeline>(SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST);

            case Lighting | Lines:
                return ioCContainer.IoCConstruct<ColorLightingPipeline>(SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_LINELIST);

            case Texture:
                return ioCContainer.IoCConstruct<SdlGpuTexturePipeline>();

            case Texture | Lighting:
                return ioCContainer.IoCConstruct<SdlGpuTextureLightingPipeline>();

            case Texture | Lighting | BumpMapping:
                return ioCContainer.IoCConstruct<SdlGpuTextureBumpLightingPipeline>();
        }

        return null;
    }

    public void Dispose()
    {
        var pipelines = _pipelines.Values.ToArray();
        _pipelines.Clear();
        
        foreach (var pipeline in pipelines)
        {
            pipeline.Dispose();
        }
    }
}