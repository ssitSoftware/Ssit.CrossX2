using SDL;
using Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics.Renderers;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Internal;
using Ssit.CrossX2.Framework.Graphics.Lighting;
using Ssit.CrossX2.Framework.Graphics.Renderers;
using Ssit.CrossX2.Framework.IoC;
using Ssit.CrossX2.Framework.Services;
using static SDL.SDL3;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics;

internal unsafe partial class SdlGpuRenderer : IRenderer, StateManager.IUpdateHwModeHandler, LightingManager.IUpdateLightsHandler, IIoCPostRegisterHandler
{
    private readonly IIoCContainer _container;
    private readonly IActionScheduler _actionScheduler;
    public SDL_GPUDevice* Device { get; }
    public SDL_Window* Window { get; }

    public Size TargetSize => CurrentOutputTarget.Size;

    public RenderPass CurrentPass { get; internal set; }
    
    public IRenderTarget CurrentRenderTarget { get; private set; }

    public void SetRenderTarget(IRenderTarget renderTarget)
    { 
        CurrentRenderTarget = renderTarget;
        var newRt = renderTarget is SdlGpuRenderTarget rt ? new(rt.Handle, rt.Size) : DefaultOutputTarget;

        if (newRt.Handle == CurrentOutputTarget.Handle)
            return;
        
        EndCurrentGpuRenderPass();
        CurrentOutputTarget = newRt;
    }

    private readonly StateManager _stateManager;
    private readonly LightingManager _lightingManager;

    public ILightingManager LightingManager => _lightingManager;
    
    public IStateManager StateManager => _stateManager;
    public IRenderStateProvider RenderStateProvider => _stateManager.StateProvider;
    public SdlGpuPrimitiveRenderer PrimitiveRenderer { get; private set; }
    
    public IGeometryRenderer GeometryRenderer => field ??= new GeometryRendererImpl(RenderQueue);
    public ISpriteRenderer SpriteRenderer => field ??= new SpriteRendererImpl(RenderQueue);
    public ITextRenderer TextRenderer => field ??= new TextRenderer(RenderQueue);
    public IRenderQueue RenderQueue { get; private set; }

    public SdlGpuRenderTargetStruct DefaultOutputTarget { get; set; }

    private SDL_GPUCommandBuffer* _commandBuffer;

    // Offscreen texture the frame is drawn into instead of the swapchain texture (see Present).
    private SDL_GPUTexture* _backBuffer;
    private Size _backBufferSize;

    /// <summary>
    /// The shared per-frame command buffer, acquired on first use. It never holds a swapchain texture
    /// until <see cref="Present"/>, so it can be submitted at any point mid-frame.
    /// </summary>
    public SDL_GPUCommandBuffer* CommandBuffer
    {
        get
        {
            if(!_actionScheduler.IsMainThread)
                throw new InvalidOperationException("Only use Sdl3GpuRenderer in main thread!");

            if (_commandBuffer is null)
            {
                _commandBuffer = SDL_AcquireGPUCommandBuffer(Device);

                if (_commandBuffer is null)
                    throw new InvalidOperationException($"SDL_AcquireGPUCommandBuffer failed: {SDL_GetError()}");
            }

            return _commandBuffer;
        }
    }

    /// <summary>
    /// Makes the offscreen back buffer (recreated when the size changes) the default output target.
    /// </summary>
    /// <returns>False when there is nothing to draw into (e.g. a zero-sized, minimized window).</returns>
    public bool PrepareBackBuffer(Size size)
    {
        if (size.Width <= 0 || size.Height <= 0)
            return false;

        if (_backBuffer is null || _backBufferSize != size)
        {
            ReleaseBackBuffer();

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

            _backBuffer = SDL_CreateGPUTexture(Device, &createInfo);

            if (_backBuffer is null)
                throw new InvalidOperationException($"SDL_CreateGPUTexture (back buffer) failed: {SDL_GetError()}");

            _backBufferSize = size;
        }

        DefaultOutputTarget = new SdlGpuRenderTargetStruct(_backBuffer, _backBufferSize);
        return true;
    }

    /// <summary>
    /// Acquires the swapchain texture on the final command buffer of the frame, copies the back buffer
    /// into it and submits, which presents. Earlier mid-frame submits never touch the swapchain.
    /// </summary>
    /// <returns>True when a swapchain frame was presented.</returns>
    public bool Present()
    {
        var startTicks = SDL_GetTicksNS();

        try
        {
            return PresentInternal();
        }
        finally
        {
            LastPresentNs = SDL_GetTicksNS() - startTicks;
        }
    }

    /// <summary>
    /// CPU time spent in the last <see cref="Present"/>, mostly waiting for the GPU / swapchain (used by perf stats).
    /// </summary>
    public ulong LastPresentNs { get; private set; }

    /// <summary>GPU render passes begun since the counter was last reset (perf stats).</summary>
    internal static int RenderPassCount;

    private bool PresentInternal()
    {
        EndCurrentGpuRenderPass();

        var commandBuffer = CommandBuffer;

        SDL_GPUTexture* swapchainTexture;
        uint swapchainWidth, swapchainHeight;

        if (!SDL_WaitAndAcquireGPUSwapchainTexture(commandBuffer, Window, &swapchainTexture, &swapchainWidth, &swapchainHeight))
        {
            Console.Error.WriteLine($"SDL_WaitAndAcquireGPUSwapchainTexture failed: {SDL_GetError()}");
            SubmitCommandBuffer();
            return false;
        }

        // Null when the window is minimized/occluded: submit the recorded work and skip presenting.
        if (swapchainTexture != null && _backBuffer != null)
        {
            var blitInfo = new SDL_GPUBlitInfo
            {
                source = new SDL_GPUBlitRegion
                {
                    texture = _backBuffer,
                    w = (uint)_backBufferSize.Width,
                    h = (uint)_backBufferSize.Height,
                },
                destination = new SDL_GPUBlitRegion
                {
                    texture = swapchainTexture,
                    w = swapchainWidth,
                    h = swapchainHeight,
                },
                load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_DONT_CARE,
                filter = SDL_GPUFilter.SDL_GPU_FILTER_LINEAR,
            };

            SDL_BlitGPUTexture(commandBuffer, &blitInfo);
        }

        SubmitCommandBuffer();
        return swapchainTexture != null;
    }

    public void ReleaseBackBuffer()
    {
        ReleaseOcclusionMask();

        if (_backBuffer is null)
            return;

        // SDL defers the actual release until command buffers using it have completed.
        SDL_ReleaseGPUTexture(Device, _backBuffer);
        _backBuffer = null;
        _backBufferSize = Size.Zero;
        DefaultOutputTarget = SdlGpuRenderTargetStruct.Null;
    }

    public SdlGpuRenderTargetStruct CurrentOutputTarget
    {
        get
        {
            if (field.Handle is null)
                return DefaultOutputTarget;

            return field;
        }
        private set;
    }
    
    public SdlGpuBackendType BackendType { get; }

    public SdlGpuRenderer(SdlHandles handles, IIoCContainer container, IActionScheduler actionScheduler)
    {
        _container = container;
        _actionScheduler = actionScheduler;
        Device = handles.GpuDevice;
        Window = handles.Window;

        var driver = SDL_GetGPUDeviceDriver(Device);
        BackendType = driver switch
        {
            "metal" => SdlGpuBackendType.Metal,
            "direct3d12" => SdlGpuBackendType.DirectX,
            "vulkan" => SdlGpuBackendType.Vulkan,
            _ => throw new ArgumentOutOfRangeException(nameof(driver), driver, null)
        };
        
        _stateManager = new StateManager(this);
        _lightingManager =  new LightingManager(this);
    }

    void IIoCPostRegisterHandler.OnAllServicesRegistered()
    {
        PrimitiveRenderer = _container.IoCConstruct<SdlGpuPrimitiveRenderer>();
        RenderQueue = new SdlGpuRenderQueue(this);
    }

    private SDL_GPURenderPass* _gpuRenderPass = null;
    
    public SDL_GPURenderPass* CurrentGpuRenderPass
    {
        get
        {
            if (_gpuRenderPass is null)
            {
                BeginNewRenderPass();
            }

            return _gpuRenderPass;
        }
    }

    public void Clear(RgbaColor color)
    {
        EndCurrentGpuRenderPass();
        BeginNewRenderPass(color);
    }
    
    internal void EndCurrentGpuRenderPass(bool flushQueue = true)
    {
        if (flushQueue)
        {
            ((IRenderQueueInternal)RenderQueue).Flush();
        }

        if (_gpuRenderPass == null)
            return;
        
        SDL_EndGPURenderPass(CurrentGpuRenderPass);
        _gpuRenderPass = null;
    }
    
    public void Flush() => EndCurrentGpuRenderPass();

    public void BeginNewRenderPass(RgbaColor? clearColor = null)
    {
        if (_gpuRenderPass != null)
            return;
        
        var clrClr =  clearColor ?? RgbaColor.Transparent;
        
        var colorTargetInfo = new SDL_GPUColorTargetInfo
        {
            clear_color = new SDL_FColor { r = clrClr.Rf, g = clrClr.Gf, b = clrClr.Bf, a = clrClr.Af },
            texture = CurrentOutputTarget.Handle,
            load_op = clearColor.HasValue ?  SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR : SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
            store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
        };

        RenderPassCount++;
        _gpuRenderPass = SDL_BeginGPURenderPass(CommandBuffer, &colorTargetInfo, 1, null);

        if (_gpuRenderPass == null)
            throw new InvalidOperationException($"SDL_BeginGPURenderPass failed: {SDL_GetError()}");

        ApplyClipRect();
    }

    private void ApplyClipRect()
    {
        var target = CurrentOutputTarget.Size;
        var clip = RenderStateProvider.ClipRect?.Intersect(new RectangleF(0, 0, target.Width, target.Height))
                   ?? new RectangleF(0, 0, target.Width, target.Height);

        var scissor = new SDL_Rect
        {
            x = (int)clip.X,
            y = (int)clip.Y,
            w = Math.Max(0, (int)clip.Width),
            h = Math.Max(0, (int)clip.Height)
        };

        SDL_SetGPUScissor(CurrentGpuRenderPass, &scissor);
    }

    public void SubmitCommandBuffer()
    {
        if (!_actionScheduler.IsMainThread)
        {
            _actionScheduler.ExecuteOnMainThread(SubmitCommandBuffer);
            return;
        }
        
        // Flushing queued draws may acquire the command buffer, so do it before checking.
        EndCurrentGpuRenderPass();

        // Check the field, not the property: reading CommandBuffer would acquire a buffer just to submit it empty.
        if (_commandBuffer is null)
            return;

        var commandBuffer = _commandBuffer;
        _commandBuffer = null;

        if (!SDL_SubmitGPUCommandBuffer(commandBuffer))
            Console.Error.WriteLine($"SDL_SubmitGPUCommandBuffer failed: {SDL_GetError()}");
    }
}