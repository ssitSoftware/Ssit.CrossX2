using SDL;
using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.Graphics.Lighting;
using Ssit.CrossX2.Graphics.Renderers;
using Ssit.CrossX2.IoC;
using static SDL.SDL3;

namespace Ssit.CrossX2._Sdl3Impl.Graphics;

internal unsafe class SdlGpuRenderer : IRenderer, StateManager.IUpdateHwModeHandler, LightingManager.IUpdateLightsHandler
{
    public SDL_GPUDevice* Device { get; }
    public SDL_Window* Window { get; }

    public Size TargetSize => CurrentOutputTarget.Size;

    public RenderPass CurrentPass { get; internal set; }

    private readonly StateManager _stateManager;
    private LightingManager _lightingManager;

    public BlendMode CurrentBlendMode { get; private set; }
    public RectangleF? CurrentClipRect { get; private set; }

    public ILightingManager LightingManager => _lightingManager;
    
    public IStateManager StateManager => _stateManager;
    public IRenderStateProvider RenderStateProvider => _stateManager;

    public IPrimitiveRenderer PrimitiveRenderer { get; private set; }
    public IGeometryRenderer GeometryRenderer { get; private set; }
    public ISpriteRenderer SpriteRenderer { get; private set; }
    public ITextRenderer TextRenderer { get; private set; }

    public SdlGpuRenderTargetStruct DefaultOutputTarget { get; set; }

    public SDL_GPUCommandBuffer* CommandBuffer
    {
        get
        {
            if (field is null)
            {
                field = SDL_AcquireGPUCommandBuffer(Device);
            }

            return field;
        }

        private set;
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

    public SdlGpuRenderer(SDL_GPUDevice* device, SDL_Window* window)
    {
        Device = device;
        Window = window;

        var driver = SDL_GetGPUDeviceDriver(device);
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

    public void Initialize(IIoCContainer container)
    {
        PrimitiveRenderer = container.IoCConstruct<SdlGpuPrimitiveRenderer>();
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

    internal void EndCurrentGpuRenderPass()
    {
        if (_gpuRenderPass == null)
            return;

        SDL_EndGPURenderPass(CurrentGpuRenderPass);
        _gpuRenderPass = null;
    }
    
    public void OnLightsUpdated()
    {
        EndCurrentGpuRenderPass();
    }

    public void UpdateHwMode(BlendMode blendMode, RectangleF? clipRect, IRenderTarget renderTarget)
    {
        EndCurrentGpuRenderPass();

        CurrentBlendMode = blendMode;
        CurrentClipRect = clipRect;

        var rt = renderTarget as SdlGpuRenderTarget;
        if (rt is null)
        {
            CurrentOutputTarget = new(null, Size.Zero);
        }
        else
        {
            CurrentOutputTarget = new(rt.Handle, rt.Size);
        }
    }

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

        _gpuRenderPass = SDL_BeginGPURenderPass(CommandBuffer, &colorTargetInfo, 1, null);

        if (_gpuRenderPass == null)
            throw new InvalidOperationException($"SDL_BeginGPURenderPass failed: {SDL_GetError()}");

        ApplyClipRect();
    }

    private void ApplyClipRect()
    {
        var target = CurrentOutputTarget.Size;
        var clip = CurrentClipRect?.Intersect(new RectangleF(0, 0, target.Width, target.Height))
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
        if (CommandBuffer is null)
            return;
        
        EndCurrentGpuRenderPass();
        SDL_SubmitGPUCommandBuffer(CommandBuffer);

        CommandBuffer = null;
    }
}