using SDL;

namespace Ssit.CrossX2._Sdl3Impl;

public unsafe class SdlHandles(SDL_Window* window, SDL_GPUDevice* device)
{
    // ReSharper disable once UnusedMember.Global
    public readonly SDL_Window* Window = window;
    public readonly SDL_GPUDevice* GpuDevice = device;
}