using SDL;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu;

internal static unsafe class DebugScreenshot
{
    private static int _frame;

    public static void MaybeCapture(SDL_GPUDevice* device, SDL_Window* window, SDL_GPUTexture* texture, uint width, uint height)
    {
        var path = Environment.GetEnvironmentVariable("CROSSX2_SCREENSHOT");
        if (path == null) return;

        _frame++;
        if (_frame != 10) return;

        var format = SDL3.SDL_GetGPUSwapchainTextureFormat(device, window);
        Console.Error.WriteLine($"[DebugScreenshot] swapchain format = {format}, size = {width}x{height}");

        uint rowBytes = width * 4;
        uint dataSize = rowBytes * height;

        var transferBufferCreateInfo = new SDL_GPUTransferBufferCreateInfo
        {
            usage = SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_DOWNLOAD,
            size = dataSize,
        };
        SDL_GPUTransferBuffer* transferBuffer = SDL3.SDL_CreateGPUTransferBuffer(device, &transferBufferCreateInfo);

        SDL_GPUCommandBuffer* commandBuffer = SDL3.SDL_AcquireGPUCommandBuffer(device);
        SDL_GPUCopyPass* copyPass = SDL3.SDL_BeginGPUCopyPass(commandBuffer);

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
        SDL3.SDL_DownloadFromGPUTexture(copyPass, &region, &transferInfo);
        SDL3.SDL_EndGPUCopyPass(copyPass);

        SDL_GPUFence* fence = SDL3.SDL_SubmitGPUCommandBufferAndAcquireFence(commandBuffer);
        SDL3.SDL_WaitForGPUFences(device, true, &fence, 1);
        SDL3.SDL_ReleaseGPUFence(device, fence);

        byte* mapped = (byte*)SDL3.SDL_MapGPUTransferBuffer(device, transferBuffer, false);

        using var fs = new FileStream(path, FileMode.Create);
        using var writer = new BinaryWriter(fs);
        var header = System.Text.Encoding.ASCII.GetBytes($"P6\n{width} {height}\n255\n");
        writer.Write(header);

        bool bgra = format == SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B8G8R8A8_UNORM;

        for (int y = 0; y < height; y++)
        {
            byte* row = mapped + y * rowBytes;
            for (int x = 0; x < width; x++)
            {
                byte b0 = row[x * 4 + 0];
                byte b1 = row[x * 4 + 1];
                byte b2 = row[x * 4 + 2];

                if (bgra)
                {
                    writer.Write(b2);
                    writer.Write(b1);
                    writer.Write(b0);
                }
                else
                {
                    writer.Write(b0);
                    writer.Write(b1);
                    writer.Write(b2);
                }
            }
        }

        SDL3.SDL_UnmapGPUTransferBuffer(device, transferBuffer);
        SDL3.SDL_ReleaseGPUTransferBuffer(device, transferBuffer);

        Console.Error.WriteLine($"[DebugScreenshot] wrote {path}");
        Environment.Exit(0);
    }
}