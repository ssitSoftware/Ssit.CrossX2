using SDL;
using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.Graphics.Lighting;
using Ssit.CrossX2.Graphics.Renderers;
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

    public IPrimitiveRenderer PrimitiveRenderer { get; }
    public IGeometryRenderer GeometryRenderer { get; }
    public ISpriteRenderer SpriteRenderer { get; }
    public ITextRenderer TextRenderer { get; }

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

    public SdlGpuRenderer(SDL_GPUDevice* device, SDL_Window* window)
    {
        Device = device;
        Window = window;

        _stateManager = new StateManager(this);
        _lightingManager =  new LightingManager(this);
        PrimitiveRenderer = new SdlGpuPrimitiveRenderer(this);
    }

    public SDL_GPURenderPass* CurrentGpuRenderPass
    {
        get
        {
            if (field is null)
            {
                BeginNewRenderPass();
            }

            return field;
        }
        private set;
    }

    public void Clear(RgbaColor black)
    {
        EndCurrentGpuRenderPass();

        var colorTargetInfo = new SDL_GPUColorTargetInfo
        {
            texture = CurrentOutputTarget.Handle,
            clear_color = new SDL_FColor { r = black.Rf, g = black.Gf, b = black.Bf, a = black.Af },
            load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
            store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
        };

        CurrentGpuRenderPass = SDL_BeginGPURenderPass(CommandBuffer, &colorTargetInfo, 1, null);

        if (CurrentGpuRenderPass == null)
            throw new InvalidOperationException($"SDL_BeginGPURenderPass failed: {SDL_GetError()}");

        EndCurrentGpuRenderPass();
    }

    internal void EndCurrentGpuRenderPass()
    {
        if (CurrentGpuRenderPass == null)
            return;

        SDL_EndGPURenderPass(CurrentGpuRenderPass);
        CurrentGpuRenderPass = null;
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

    public void BeginNewRenderPass()
    {
        if (CurrentGpuRenderPass != null)
            return;

        var colorTargetInfo = new SDL_GPUColorTargetInfo
        {
            texture = CurrentOutputTarget.Handle,
            load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
            store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
        };

        CurrentGpuRenderPass = SDL_BeginGPURenderPass(CommandBuffer, &colorTargetInfo, 1, null);

        if (CurrentGpuRenderPass == null)
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

        SDL_SubmitGPUCommandBuffer(CommandBuffer);
        CommandBuffer = null;
    }
}