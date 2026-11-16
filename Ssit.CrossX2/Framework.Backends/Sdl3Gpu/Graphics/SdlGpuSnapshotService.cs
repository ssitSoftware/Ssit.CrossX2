using SDL;
using SkiaSharp;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Services;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics;

/// <summary>
/// Reads a texture back from the GPU and encodes it as JPEG with Skia.
/// </summary>
internal unsafe class SdlGpuSnapshotService(SdlGpuRenderer renderer, IActionScheduler actionScheduler) : ISnapshotService
{
    private const int JpegQuality = 90;

    public void GetSnapshot(Stream outStream, ITexture tex, float downSizeTimes)
    {
        ArgumentNullException.ThrowIfNull(outStream);
        ArgumentOutOfRangeException.ThrowIfLessThan(downSizeTimes, 1f);

        if (tex is not ISdlGpuTexture gpuTexture)
            throw new ArgumentException("Texture was not created by the SDL GPU backend.", nameof(tex));

        if (!actionScheduler.IsMainThread)
            throw new InvalidOperationException("Snapshots can only be taken on the main thread.");

        var handle = gpuTexture.GetMap(TextureMaps.Diffuse);
        if (handle == null)
            throw new ArgumentException("Texture has no diffuse map.", nameof(tex));

        var colorType = gpuTexture.Format switch
        {
            SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B8G8R8A8_UNORM => SKColorType.Bgra8888,
            SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM => SKColorType.Rgba8888,
            _ => throw new NotSupportedException($"Texture format {gpuTexture.Format} is not supported for snapshots.")
        };

        var bitmap = Download(handle, tex.Size, colorType);
        SKBitmap scaled = null;

        try
        {
            scaled = Downscale(bitmap, downSizeTimes);

            if (!scaled.Encode(outStream, SKEncodedImageFormat.Jpeg, JpegQuality))
                throw new InvalidOperationException("Failed to encode the snapshot as JPEG.");
        }
        finally
        {
            if (scaled != null && !ReferenceEquals(scaled, bitmap))
            {
                scaled.Dispose();
            }

            bitmap.Dispose();
        }
    }

    /// <summary>
    /// Copies the texture into a Skia bitmap. Blocks until the GPU has finished the copy.
    /// </summary>
    private SKBitmap Download(SDL_GPUTexture* texture, Size size, SKColorType colorType)
    {
        var device = renderer.Device;
        var width = (uint)size.Width;
        var height = (uint)size.Height;
        var dataSize = width * height * 4;

        // Commands recorded so far must reach the GPU before the texture is read.
        renderer.SubmitCommandBuffer();

        var transferBufferCreateInfo = new SDL_GPUTransferBufferCreateInfo
        {
            usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_DOWNLOAD,
            size = dataSize,
        };

        var transferBuffer = SDL_CreateGPUTransferBuffer(device, &transferBufferCreateInfo);
        if (transferBuffer == null)
            throw new InvalidOperationException($"SDL_CreateGPUTransferBuffer failed: {SDL_GetError()}");

        try
        {
            var commandBuffer = SDL_AcquireGPUCommandBuffer(device);
            if (commandBuffer == null)
                throw new InvalidOperationException($"SDL_AcquireGPUCommandBuffer failed: {SDL_GetError()}");

            var copyPass = SDL_BeginGPUCopyPass(commandBuffer);

            var region = new SDL_GPUTextureRegion
            {
                texture = texture,
                mip_level = 0,
                layer = 0,
                x = 0,
                y = 0,
                z = 0,
                w = width,
                h = height,
                d = 1,
            };

            var transferInfo = new SDL_GPUTextureTransferInfo
            {
                transfer_buffer = transferBuffer,
                offset = 0,
                pixels_per_row = width,
                rows_per_layer = height,
            };

            SDL_DownloadFromGPUTexture(copyPass, &region, &transferInfo);
            SDL_EndGPUCopyPass(copyPass);

            var fence = SDL_SubmitGPUCommandBufferAndAcquireFence(commandBuffer);
            if (fence == null)
                throw new InvalidOperationException($"SDL_SubmitGPUCommandBufferAndAcquireFence failed: {SDL_GetError()}");

            SDL_WaitForGPUFences(device, true, &fence, 1);
            SDL_ReleaseGPUFence(device, fence);

            var mapped = (byte*)SDL_MapGPUTransferBuffer(device, transferBuffer, false);
            if (mapped == null)
                throw new InvalidOperationException($"SDL_MapGPUTransferBuffer failed: {SDL_GetError()}");

            try
            {
                // Alpha of the render target isn't meaningful for a screenshot - treat the image as opaque.
                var bitmap = new SKBitmap(new SKImageInfo((int)width, (int)height, colorType, SKAlphaType.Opaque));
                Buffer.MemoryCopy(mapped, (void*)bitmap.GetPixels(), bitmap.ByteCount, dataSize);
                return bitmap;
            }
            finally
            {
                SDL_UnmapGPUTransferBuffer(device, transferBuffer);
            }
        }
        finally
        {
            SDL_ReleaseGPUTransferBuffer(device, transferBuffer);
        }
    }

    /// <summary>
    /// Shrinks the bitmap <paramref name="times"/> times with the best quality: halves it with linear filtering
    /// while it is still at least twice too big (at exactly half size that averages 2x2 pixels, so no pixel is skipped),
    /// then resamples the rest with the Mitchell cubic filter. Returns <paramref name="source"/> when the size doesn't change.
    /// <paramref name="times"/> may be fractional (e.g. 2.5): the target size is rounded to whole pixels.
    /// </summary>
    private static SKBitmap Downscale(SKBitmap source, float times)
    {
        var targetWidth = Math.Max(1, (int)MathF.Round(source.Width / times));
        var targetHeight = Math.Max(1, (int)MathF.Round(source.Height / times));

        if (targetWidth == source.Width && targetHeight == source.Height)
            return source;

        var current = source;

        while (current.Width >= targetWidth * 2 && current.Height >= targetHeight * 2)
        {
            var half = Resize(current, current.Width / 2, current.Height / 2, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
            
            if (!ReferenceEquals(current, source))
            {
                current.Dispose();
            }

            current = half;
        }

        if (current.Width != targetWidth || current.Height != targetHeight)
        {
            var final = Resize(current, targetWidth, targetHeight, new SKSamplingOptions(SKCubicResampler.Mitchell));
            
            if (!ReferenceEquals(current, source))
            {
                current.Dispose();
            }

            current = final;
        }

        return current;
    }

    private static SKBitmap Resize(SKBitmap bitmap, int width, int height, SKSamplingOptions sampling)
    {
        var info = new SKImageInfo(width, height, bitmap.ColorType, SKAlphaType.Opaque);
        return bitmap.Resize(info, sampling) ?? throw new InvalidOperationException("Failed to resize the snapshot.");
    }
}
