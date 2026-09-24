using SDL;
using Ssit.CrossX2.Framework.Graphics;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu.Graphics;

internal static class SdlGpuBlendStateFactory
{
    public static readonly BlendMode[] AllModes =
    [
        BlendMode.None,
        BlendMode.AlphaBlend,
        BlendMode.Additive,
        BlendMode.Multiply,
    ];

    public static SDL_GPUColorTargetBlendState Create(BlendMode blendMode) => blendMode switch
    {
        BlendMode.None => new SDL_GPUColorTargetBlendState
        {
            enable_blend = false,
        },

        BlendMode.Additive => new SDL_GPUColorTargetBlendState
        {
            enable_blend = true,
            src_color_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
            dst_color_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
            color_blend_op = SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
            src_alpha_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
            dst_alpha_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
            alpha_blend_op = SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
        },

        BlendMode.Multiply => new SDL_GPUColorTargetBlendState
        {
            enable_blend = true,
            src_color_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_DST_COLOR,
            dst_color_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ZERO,
            color_blend_op = SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
            src_alpha_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_DST_ALPHA,
            dst_alpha_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ZERO,
            alpha_blend_op = SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
        },

        // BlendMode.AlphaBlend and default
        _ => new SDL_GPUColorTargetBlendState
        {
            enable_blend = true,
            src_color_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
            dst_color_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,
            color_blend_op = SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
            src_alpha_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
            dst_alpha_blendfactor = SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,
            alpha_blend_op = SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
        },
    };
}
