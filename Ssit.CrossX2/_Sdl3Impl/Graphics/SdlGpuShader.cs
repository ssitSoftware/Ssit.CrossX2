using System.Text;
using SDL;
using static SDL.SDL3;

namespace Ssit.CrossX2._Sdl3Impl.Graphics;

static unsafe class GpuShader
{
    public static SDL_GPUShader* CreateFromEmbeddedResource(SDL_GPUDevice* device, string resourceName, string entrypoint, SDL_GPUShaderStage stage, int numSamplers = 0, int numUniformBuffers = 0)
    {
        byte[] code = Encoding.UTF8.GetBytes(LoadEmbeddedSource(resourceName));
        byte[] entrypointBytes = Encoding.UTF8.GetBytes(entrypoint + "\0");

        fixed (byte* codePtr = code)
        fixed (byte* entrypointPtr = entrypointBytes)
        {
            var createInfo = new SDL_GPUShaderCreateInfo
            {
                code_size = (nuint)code.Length,
                code = codePtr,
                entrypoint = entrypointPtr,
                format = SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL,
                stage = stage,
                num_samplers = (uint)numSamplers,
                num_storage_textures = 0,
                num_storage_buffers = 0,
                num_uniform_buffers = (uint)numUniformBuffers,
            };

            return SDL_CreateGPUShader(device, &createInfo);
        }
    }

    private static string LoadEmbeddedSource(string resourceName)
    {
        using Stream stream = typeof(GpuShader).Assembly.GetManifestResourceStream(resourceName)
                              ?? throw new FileNotFoundException($"Embedded shader resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}