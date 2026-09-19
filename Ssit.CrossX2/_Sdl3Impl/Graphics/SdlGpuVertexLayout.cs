using System.Numerics;
using SDL;
using Ssit.CrossX2.Graphics;

namespace Ssit.CrossX2._Sdl3Impl.Graphics;

internal static unsafe class GpuVertexLayout
{
    public static int GetStrideBytes(VertexComponents components)
    {
        int stride = 0;

        if (components.HasFlag(VertexComponents.Position))
            stride += sizeof(Vector3);

        if (components.HasFlag(VertexComponents.Color))
            stride += sizeof(RgbaColor);

        if (components.HasFlag(VertexComponents.Texture))
            stride += sizeof(Vector2);

        if (components.HasFlag(VertexComponents.Tangent))
            stride += sizeof(Vector2);

        if (components.HasFlag(VertexComponents.BiNormal))
            stride += sizeof(Vector2);

        return stride;
    }

    public static SDL_GPUVertexBufferDescription CreateVertexBufferDescription(VertexComponents components, uint slot = 0)
    {
        return new SDL_GPUVertexBufferDescription
        {
            slot = slot,
            pitch = (uint)GetStrideBytes(components),
            input_rate = SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_VERTEX,
            instance_step_rate = 0,
        };
    }

    public static SDL_GPUVertexAttribute[] CreateVertexAttributes(VertexComponents components, uint bufferSlot = 0)
    {
        int count = 0;
        if (components.HasFlag(VertexComponents.Position)) count++;
        if (components.HasFlag(VertexComponents.Color)) count++;
        if (components.HasFlag(VertexComponents.Texture)) count++;
        if (components.HasFlag(VertexComponents.Tangent)) count++;
        if (components.HasFlag(VertexComponents.BiNormal)) count++;

        var attributes = new SDL_GPUVertexAttribute[count];

        uint location = 0;
        uint offset = 0;

        if (components.HasFlag(VertexComponents.Position))
        {
            attributes[location] = new SDL_GPUVertexAttribute
            {
                location = location,
                buffer_slot = bufferSlot,
                format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
                offset = offset,
            };
            location++;
            offset += (uint)sizeof(Vector3);
        }

        if (components.HasFlag(VertexComponents.Color))
        {
            attributes[location] = new SDL_GPUVertexAttribute
            {
                location = location,
                buffer_slot = bufferSlot,
                format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4_NORM,
                offset = offset,
            };
            location++;
            offset += (uint)sizeof(RgbaColor);
        }

        if (components.HasFlag(VertexComponents.Texture))
        {
            attributes[location] = new SDL_GPUVertexAttribute
            {
                location = location,
                buffer_slot = bufferSlot,
                format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
                offset = offset,
            };
            location++;
            offset += (uint)sizeof(Vector2);
        }

        if (components.HasFlag(VertexComponents.Tangent))
        {
            attributes[location] = new SDL_GPUVertexAttribute
            {
                location = location,
                buffer_slot = bufferSlot,
                format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
                offset = offset,
            };
            location++;
            offset += (uint)sizeof(Vector2);
        }

        if (components.HasFlag(VertexComponents.BiNormal))
        {
            attributes[location] = new SDL_GPUVertexAttribute
            {
                location = location,
                buffer_slot = bufferSlot,
                format = SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
                offset = offset,
            };
            location++;
            offset += (uint)sizeof(Vector2);
        }

        return attributes;
    }
}
