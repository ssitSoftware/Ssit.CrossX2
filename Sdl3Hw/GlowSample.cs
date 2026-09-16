using System.Numerics;
using System.Reflection;
using SDL;
using static SDL.SDL3;
using static SDL.SDL3_image;

namespace Sdl3Hw;

public sealed unsafe class GlowSample : ISample
{
    private readonly SDL_GPUDevice* _device;
    private readonly SDL_Window* _window;

    private readonly GlowEffect _glowEffect;
    private readonly CrtSimEffect _crtSimEffect;

    private readonly SDL_GPUTexture* _baseTexture;
    private readonly SDL_GPUTexture* _glowTexture;
    private readonly SDL_GPUTexture* _outputTexture;

    private readonly Vector2 _outputSize;

    public float Intensity { get; set; } = 2.0f;

    public float BarrelDistortion { get => _crtSimEffect.BarrelDistortion; set => _crtSimEffect.BarrelDistortion = value; }
    public float RgbDisplacement { get => _crtSimEffect.RgbDisplacement; set => _crtSimEffect.RgbDisplacement = value; }
    public float ScanlineIntensity { get => _crtSimEffect.ScanlineIntensity; set => _crtSimEffect.ScanlineIntensity = value; }
    public float Vignette { get => _crtSimEffect.Vignette; set => _crtSimEffect.Vignette = value; }

    public GlowSample(SDL_GPUDevice* device, SDL_Window* window)
    {
        _device = device;
        _window = window;

        SDL_GPUTextureFormat targetFormat = SDL_GetGPUSwapchainTextureFormat(device, window);

        SDL_GPUCommandBuffer* uploadCommandBuffer = SDL_AcquireGPUCommandBuffer(device);
        SDL_GPUCopyPass* copyPass = SDL_BeginGPUCopyPass(uploadCommandBuffer);

        byte[] baseBytes = LoadEmbeddedBytes("Assets.Img.png");
        byte[] glowBytes = LoadEmbeddedBytes("Assets.Img.glow.png");

        int baseWidth, baseHeight, glowWidth, glowHeight;

        fixed (byte* basePtr = baseBytes)
        {
            SDL_IOStream* io = SDL_IOFromConstMem((IntPtr)basePtr, (nuint)baseBytes.Length);
            _baseTexture = IMG_LoadGPUTexture_IO(device, copyPass, io, true, &baseWidth, &baseHeight);
        }

        fixed (byte* glowPtr = glowBytes)
        {
            SDL_IOStream* io = SDL_IOFromConstMem((IntPtr)glowPtr, (nuint)glowBytes.Length);
            _glowTexture = IMG_LoadGPUTexture_IO(device, copyPass, io, true, &glowWidth, &glowHeight);
        }

        if (_baseTexture == null || _glowTexture == null)
        {
            throw new InvalidOperationException($"IMG_LoadGPUTexture_IO failed: {SDL_GetError()}");
        }

        _outputSize = new Vector2(baseWidth, baseHeight);

        var outputTextureCreateInfo = new SDL_GPUTextureCreateInfo
        {
            type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D,
            format = targetFormat,
            usage = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER | SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COLOR_TARGET,
            width = (uint)baseWidth,
            height = (uint)baseHeight,
            layer_count_or_depth = 1,
            num_levels = 1,
            sample_count = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1,
        };
        _outputTexture = SDL_CreateGPUTexture(device, &outputTextureCreateInfo);

        if (_outputTexture == null)
        {
            throw new InvalidOperationException($"SDL_CreateGPUTexture failed: {SDL_GetError()}");
        }

        SDL_EndGPUCopyPass(copyPass);
        SDL_SubmitGPUCommandBuffer(uploadCommandBuffer);

        _glowEffect = new GlowEffect(device, targetFormat, glowWidth, glowHeight);
        _crtSimEffect = new CrtSimEffect(device, targetFormat);
    }

    public void Render()
    {
        SDL_GPUCommandBuffer* commandBuffer = SDL_AcquireGPUCommandBuffer(_device);

        _glowEffect.Render(commandBuffer, _baseTexture, _glowTexture, _outputTexture, Intensity);

        SDL_GPUTexture* swapchainTexture;
        uint swapchainWidth, swapchainHeight;

        if (!SDL_WaitAndAcquireGPUSwapchainTexture(commandBuffer, _window, &swapchainTexture, &swapchainWidth, &swapchainHeight))
        {
            Console.Error.WriteLine($"SDL_WaitAndAcquireGPUSwapchainTexture failed: {SDL_GetError()}");
            SDL_SubmitGPUCommandBuffer(commandBuffer);
            return;
        }

        if (swapchainTexture != null)
        {
            var clearColor = new SDL_FColor { r = 0.05f, g = 0.05f, b = 0.08f, a = 1.0f };

            _crtSimEffect.Render(commandBuffer, _outputTexture, (int)_outputSize.X, (int)_outputSize.Y,
                swapchainTexture, swapchainWidth, swapchainHeight, clearColor);
        }

        SDL_SubmitGPUCommandBuffer(commandBuffer);
    }

    public void Dispose()
    {
        _glowEffect.Dispose();
        _crtSimEffect.Dispose();

        SDL_ReleaseGPUTexture(_device, _baseTexture);
        SDL_ReleaseGPUTexture(_device, _glowTexture);
        SDL_ReleaseGPUTexture(_device, _outputTexture);
    }

    private static byte[] LoadEmbeddedBytes(string resourceName)
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Embedded resource '{resourceName}' not found.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
