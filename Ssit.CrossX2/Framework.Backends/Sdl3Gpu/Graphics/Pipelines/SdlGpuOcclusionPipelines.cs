using System.Numerics;
using SDL;
using Ssit.CrossX2.Framework.Graphics;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics.Pipelines;

/// <summary>
/// Draws textured triangles as flat silhouettes (coverage of <see cref="SdlGpuRenderer.SilhouetteMap"/> filled with
/// <see cref="SdlGpuRenderer.SilhouetteColor"/>) into the occlusion mask.
/// </summary>
internal sealed unsafe class SdlGpuSilhouettePipeline : SdlGpuTexturePipeline
{
    // Bound for textures without the collision mask - they draw nothing.
    private readonly SDL_GPUTexture* _emptyMask;

    public SdlGpuSilhouettePipeline(SdlGpuRenderer renderer)
        : base(renderer,
            "Pipelines.Texture.vert",
            "Pipelines.Texture.frag", fragmentUniformBuffers: 1, fragmentSamplers: 1, fragmentDefine: "SILHOUETTE")
    {
        _emptyMask = GpuTextureHelper.CreateSolidColorTexture(_device, 0, 0, 0, 0);
    }

    public override void Bind(SDL_GPUCommandBuffer* commandBuffer, SDL_GPURenderPass* renderPass, SDL_GPUTexture*[] textures, Matrix4x4 transform)
    {
        base.Bind(commandBuffer, renderPass, textures, transform);

        if (GpuRenderer.SilhouetteMap == TextureMaps.CollisionMask)
        {
            // The collision mask replaces the diffuse map (same layout, so the same texture coordinates).
            var mask = textures.Length > ISdlGpuPipeline.CollisionMaskTexture ? textures[ISdlGpuPipeline.CollisionMaskTexture] : null;

            var maskBinding = stackalloc SDL_GPUTextureSamplerBinding[1];
            maskBinding[0] = new SDL_GPUTextureSamplerBinding { texture = mask != null ? mask : _emptyMask, sampler = PointSampler };
            SDL_BindGPUFragmentSamplers(renderPass, 0, maskBinding, 1);
        }

        var color = GpuRenderer.SilhouetteColor;
        SDL_PushGPUFragmentUniformData(commandBuffer, 0, (IntPtr)(&color), (uint)sizeof(Vector4));
    }

    public override void Dispose()
    {
        base.Dispose();
        SDL_ReleaseGPUTexture(_device, _emptyMask);
    }
}

/// <summary>
/// Unlit textured triangles drawing the contours of the occlusion mask silhouettes they cover.
/// </summary>
internal sealed unsafe class SdlGpuTextureContourPipeline : SdlGpuTexturePipeline
{
    public SdlGpuTextureContourPipeline(SdlGpuRenderer renderer)
        : base(renderer,
            "Pipelines.Texture.vert",
            "Pipelines.Texture.frag", fragmentUniformBuffers: 1, fragmentSamplers: 2, fragmentDefine: "OCCLUSION_CONTOUR")
    {
    }

    public override void Bind(SDL_GPUCommandBuffer* commandBuffer, SDL_GPURenderPass* renderPass, SDL_GPUTexture*[] textures, Matrix4x4 transform)
    {
        base.Bind(commandBuffer, renderPass, textures, transform);
        SdlGpuOcclusionUniforms.Push(GpuRenderer, commandBuffer, renderPass, PointSampler, samplerSlot: 1, uniformSlot: 0);
    }
}

/// <summary>
/// Lit textured triangles drawing the contours of the occlusion mask silhouettes they cover.
/// </summary>
internal sealed unsafe class SdlGpuTextureLightingContourPipeline : SdlGpuTextureLightingPipeline
{
    public SdlGpuTextureLightingContourPipeline(SdlGpuRenderer renderer)
        : base(renderer, fragmentUniformBuffers: 2, fragmentSamplers: 3, fragmentDefine: "OCCLUSION_CONTOUR")
    {
    }

    public override void Bind(SDL_GPUCommandBuffer* commandBuffer, SDL_GPURenderPass* renderPass, SDL_GPUTexture*[] textures, Matrix4x4 transform)
    {
        base.Bind(commandBuffer, renderPass, textures, transform);
        SdlGpuOcclusionUniforms.Push(GpuRenderer, commandBuffer, renderPass, PointSampler, samplerSlot: 2, uniformSlot: 1);
    }
}

internal static unsafe class SdlGpuOcclusionUniforms
{
    /// <summary>
    /// Binds the occlusion mask (point sampled - hard pixel edges) and pushes the contour parameters.
    /// </summary>
    public static void Push(SdlGpuRenderer renderer, SDL_GPUCommandBuffer* commandBuffer, SDL_GPURenderPass* renderPass,
        SDL_GPUSampler* sampler, uint samplerSlot, uint uniformSlot)
    {
        var maskBinding = stackalloc SDL_GPUTextureSamplerBinding[1];
        maskBinding[0] = new SDL_GPUTextureSamplerBinding { texture = renderer.OcclusionMask, sampler = sampler };
        SDL_BindGPUFragmentSamplers(renderPass, samplerSlot, maskBinding, 1);

        var parameters = renderer.OcclusionParameters;
        SDL_PushGPUFragmentUniformData(commandBuffer, uniformSlot, (IntPtr)(&parameters), (uint)sizeof(Vector4));
    }
}
