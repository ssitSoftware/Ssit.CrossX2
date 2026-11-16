using System.Diagnostics;
using System.Text;
using SDL;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics;

internal static unsafe class GpuShader
{
    public static SDL_GPUShader* CreateFromEmbeddedResource(SdlGpuRenderer renderer, string resourceName, string entrypoint, SDL_GPUShaderStage stage, int numSamplers = 0, int numUniformBuffers = 0)
    {
        const string resourcePrefix = "Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Assets.Shaders.";

        SDL_GPUShaderFormat format;
        byte[] code;

        switch (renderer.BackendType)
        {
            case SdlGpuBackendType.Metal:
                format = SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL;
                code = Encoding.UTF8.GetBytes(LoadEmbeddedSource(resourcePrefix + "Metal." + resourceName + ".metal"));
                break;

            case SdlGpuBackendType.DirectX:
            {
                // HLSL is compiled at runtime to DXBC (SM 5.1), which SDL's Direct3D 12 backend accepts.
                format = SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXBC;
                var hlslResource = resourcePrefix + "HLSL." + resourceName + ".hlsl";
                var target = stage == SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX ? "vs_5_1" : "ps_5_1";
                code = D3DShaderCompiler.Compile(LoadEmbeddedSource(hlslResource), hlslResource, entrypoint, target);
                break;
            }

            case SdlGpuBackendType.Vulkan:
                format = SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV;
                code = LoadEmbeddedBytes(resourcePrefix + "SPIRV." + resourceName + ".spv");
                break;

            default:
                throw new NotSupportedException($"Shader creation is not supported for backend type {renderer.BackendType}");
        }

        byte[] entrypointBytes = Encoding.UTF8.GetBytes(entrypoint + "\0");

        fixed (byte* codePtr = code)
        fixed (byte* entrypointPtr = entrypointBytes)
        {
            var createInfo = new SDL_GPUShaderCreateInfo
            {
                code_size = (nuint)code.Length,
                code = codePtr,
                entrypoint = entrypointPtr,
                format = format,
                stage = stage,
                num_samplers = (uint)numSamplers,
                num_storage_textures = 0,
                num_storage_buffers = 0,
                num_uniform_buffers = (uint)numUniformBuffers,
            };

            var shader = SDL_CreateGPUShader(renderer.Device, &createInfo);

            if (shader == null)
            {
                var error = SDL_GetError();
                Console.WriteLine($"SDL_CreateGPUShader failed: {error}");
                Debugger.Break();
            }
            
            return shader;
        }
    }

    private static byte[] LoadEmbeddedBytes(string resourceName)
    {
        using Stream stream = typeof(GpuShader).Assembly.GetManifestResourceStream(resourceName)
                              ?? throw new FileNotFoundException($"Embedded shader resource '{resourceName}' not found.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static string LoadEmbeddedSource(string resourceName)
    {
        using Stream stream = typeof(GpuShader).Assembly.GetManifestResourceStream(resourceName)
                              ?? throw new FileNotFoundException($"Embedded shader resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}