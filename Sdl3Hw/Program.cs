using Sdl3Hw;
using SDL;
using static SDL.SDL3;

unsafe
{
    if (!SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO))
    {
        Console.Error.WriteLine($"SDL_Init failed: {SDL_GetError()}");
        return;
    }

    SDL_Window* window = SDL_CreateWindow("SDL# GPU Samples"u8, 800, 600, SDL_WindowFlags.SDL_WINDOW_RESIZABLE);

    if (window == null)
    {
        Console.Error.WriteLine($"SDL_CreateWindow failed: {SDL_GetError()}");
        return;
    }

    SDL_GPUDevice* device = SDL_CreateGPUDevice(SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL, true, (byte*)null);

    if (device == null)
    {
        Console.Error.WriteLine($"SDL_CreateGPUDevice failed: {SDL_GetError()}");
        return;
    }

    if (!SDL_ClaimWindowForGPUDevice(device, window))
    {
        Console.Error.WriteLine($"SDL_ClaimWindowForGPUDevice failed: {SDL_GetError()}");
        return;
    }

    using (var sample = new GlowSample(device, window))
    {
        bool running = true;
        while (running)
        {
            SDL_Event sdlEvent;
            while (SDL_PollEvent(&sdlEvent))
            {
                if (sdlEvent.Type is SDL_EventType.SDL_EVENT_QUIT or SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED)
                {
                    running = false;
                }
            }

            sample.Render();
        }
    }

    SDL_ReleaseWindowFromGPUDevice(device, window);
    SDL_DestroyGPUDevice(device);
    SDL_DestroyWindow(window);
    SDL_Quit();
}
