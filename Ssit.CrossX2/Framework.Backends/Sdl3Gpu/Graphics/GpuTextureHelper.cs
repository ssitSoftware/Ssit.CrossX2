using SDL;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics;

internal static unsafe class GpuTextureHelper
{
    public static SDL_GPUTexture* CreateSolidColorTexture(SDL_GPUDevice* device, byte r, byte g, byte b, byte a)
    {
        var textureCreateInfo = new SDL_GPUTextureCreateInfo
        {
            type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D,
            format = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM,
            usage = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER,
            width = 1,
            height = 1,
            layer_count_or_depth = 1,
            num_levels = 1,
            sample_count = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1,
        };

        SDL_GPUTexture* texture = SDL_CreateGPUTexture(device, &textureCreateInfo);

        if (texture == null)
        {
            throw new InvalidOperationException($"SDL_CreateGPUTexture failed: {SDL_GetError()}");
        }

        var transferBufferCreateInfo = new SDL_GPUTransferBufferCreateInfo
        {
            usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
            size = 4,
        };
        SDL_GPUTransferBuffer* transferBuffer = SDL_CreateGPUTransferBuffer(device, &transferBufferCreateInfo);

        if (transferBuffer == null)
        {
            SDL_ReleaseGPUTexture(device, texture);
            throw new InvalidOperationException($"SDL_CreateGPUTransferBuffer failed: {SDL_GetError()}");
        }

        byte* mapped = (byte*)SDL_MapGPUTransferBuffer(device, transferBuffer, false);
        mapped[0] = r;
        mapped[1] = g;
        mapped[2] = b;
        mapped[3] = a;
        SDL_UnmapGPUTransferBuffer(device, transferBuffer);

        SDL_GPUCommandBuffer* commandBuffer = SDL_AcquireGPUCommandBuffer(device);
        SDL_GPUCopyPass* copyPass = SDL_BeginGPUCopyPass(commandBuffer);

        var transferInfo = new SDL_GPUTextureTransferInfo
        {
            transfer_buffer = transferBuffer,
            offset = 0,
            pixels_per_row = 1,
            rows_per_layer = 1,
        };
        var region = new SDL_GPUTextureRegion
        {
            texture = texture,
            mip_level = 0,
            layer = 0,
            x = 0,
            y = 0,
            z = 0,
            w = 1,
            h = 1,
            d = 1,
        };
        SDL_UploadToGPUTexture(copyPass, &transferInfo, &region, false);

        SDL_EndGPUCopyPass(copyPass);
        SDL_SubmitGPUCommandBuffer(commandBuffer);

        SDL_ReleaseGPUTransferBuffer(device, transferBuffer);

        return texture;
    }
}
