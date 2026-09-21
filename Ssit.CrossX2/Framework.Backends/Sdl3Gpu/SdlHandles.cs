using SDL;

namespace Ssit.CrossX2.Framework.Backends.Sdl3Gpu;

internal unsafe class SdlHandles(SDL_Window* window, SDL_GPUDevice* device)
{
    // ReSharper disable once UnusedMember.Global
    public readonly SDL_Window* Window = window;
    public readonly SDL_GPUDevice* GpuDevice = device;
}