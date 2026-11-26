using System.Numerics;
using SDL;
using Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics.Renderers;
using Ssit.CrossX2.Framework.Graphics;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics;

/// <summary>
/// What the textured pipelines do with the occlusion mask (see <see cref="IOcclusionContours"/>).
/// </summary>
internal enum SdlGpuOcclusionMode
{
    None,
    /// <summary>Drawing silhouettes into the occlusion mask.</summary>
    Silhouette,
    /// <summary>Drawing the contours of the mask silhouettes over the covering pixels.</summary>
    Contour
}

internal unsafe partial class SdlGpuRenderer : IOcclusionContours
{
    private SDL_GPUTexture* _occlusionMask;
    private Size _occlusionMaskSize;
    private SdlGpuRenderTargetStruct _occlusionReturnTarget;

    public IOcclusionContours OcclusionContours => this;

    internal SdlGpuOcclusionMode OcclusionMode { get; private set; }

    internal SDL_GPUTexture* OcclusionMask => _occlusionMask;

    /// <summary>
    /// Straight-alpha color of the silhouettes drawn into the mask.
    /// </summary>
    internal Vector4 SilhouetteColor { get; private set; } = Vector4.One;

    /// <summary>
    /// Texture map the silhouettes are drawn from (<see cref="TextureMaps.Diffuse"/> or <see cref="TextureMaps.CollisionMask"/>).
    /// </summary>
    internal TextureMaps SilhouetteMap { get; private set; } = TextureMaps.Diffuse;

    /// <summary>
    /// xy = 1 / mask size in pixels, z = contour width in pixels, w = contour opacity.
    /// </summary>
    internal Vector4 OcclusionParameters { get; private set; }

    public void BeginMask()
    {
        if (OcclusionMode != SdlGpuOcclusionMode.None)
            throw new InvalidOperationException($"{nameof(BeginMask)} called while occlusion mode is {OcclusionMode}.");

        var size = CurrentOutputTarget.Size;

        // Draws queued for the current target go there first.
        EndCurrentGpuRenderPass();

        EnsureOcclusionMask(size);

        _occlusionReturnTarget = CurrentOutputTarget;
        CurrentOutputTarget = new SdlGpuRenderTargetStruct(_occlusionMask, size);

        SilhouetteColor = Vector4.One;
        SilhouetteMap = TextureMaps.Diffuse;
        OcclusionMode = SdlGpuOcclusionMode.Silhouette;

        BeginNewRenderPass(RgbaColor.Transparent);
    }

    public void SetSilhouetteColor(RgbaColor color)
    {
        var value = new Vector4(color.Rf, color.Gf, color.Bf, color.Af);

        if (value == SilhouetteColor)
            return;

        // Queued silhouettes keep the previous color.
        ((IRenderQueueInternal)RenderQueue).Flush();
        SilhouetteColor = value;
    }

    public void SetSilhouetteMap(TextureMaps map)
    {
        if (map is not (TextureMaps.Diffuse or TextureMaps.CollisionMask))
            throw new ArgumentOutOfRangeException(nameof(map), map, "Silhouettes are drawn from the diffuse map or the collision mask.");

        if (map == SilhouetteMap)
            return;

        // Queued silhouettes keep the previous map.
        ((IRenderQueueInternal)RenderQueue).Flush();
        SilhouetteMap = map;
    }

    public void EndMask()
    {
        if (OcclusionMode != SdlGpuOcclusionMode.Silhouette)
            return;

        EndCurrentGpuRenderPass();

        OcclusionMode = SdlGpuOcclusionMode.None;
        CurrentOutputTarget = _occlusionReturnTarget;
        _occlusionReturnTarget = SdlGpuRenderTargetStruct.Null;
    }

    public bool BeginContours(float width, float opacity)
    {
        if (OcclusionMode != SdlGpuOcclusionMode.None)
            throw new InvalidOperationException($"{nameof(BeginContours)} called while occlusion mode is {OcclusionMode}.");

        var size = CurrentOutputTarget.Size;

        if (_occlusionMask is null || _occlusionMaskSize != size || opacity <= 0)
            return false;

        // Queued draws are not contoured.
        ((IRenderQueueInternal)RenderQueue).Flush();

        OcclusionParameters = new Vector4(1f / size.Width, 1f / size.Height, MathF.Max(1f, width), Math.Clamp(opacity, 0f, 1f));
        OcclusionMode = SdlGpuOcclusionMode.Contour;
        return true;
    }

    public void EndContours()
    {
        if (OcclusionMode != SdlGpuOcclusionMode.Contour)
            return;

        ((IRenderQueueInternal)RenderQueue).Flush();
        OcclusionMode = SdlGpuOcclusionMode.None;
    }

    private void EnsureOcclusionMask(Size size)
    {
        if (_occlusionMask is not null && _occlusionMaskSize == size)
            return;

        ReleaseOcclusionMask();

        var createInfo = new SDL_GPUTextureCreateInfo
        {
            type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D,
            format = SDL_GetGPUSwapchainTextureFormat(Device, Window),
            usage = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COLOR_TARGET | SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER,
            width = (uint)size.Width,
            height = (uint)size.Height,
            layer_count_or_depth = 1,
            num_levels = 1,
            sample_count = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1,
        };

        _occlusionMask = SDL_CreateGPUTexture(Device, &createInfo);

        if (_occlusionMask is null)
            throw new InvalidOperationException($"SDL_CreateGPUTexture (occlusion mask) failed: {SDL_GetError()}");

        _occlusionMaskSize = size;
    }

    private void ReleaseOcclusionMask()
    {
        if (_occlusionMask is null)
            return;

        // SDL defers the actual release until command buffers using it have completed.
        SDL_ReleaseGPUTexture(Device, _occlusionMask);
        _occlusionMask = null;
        _occlusionMaskSize = Size.Zero;
    }
}
