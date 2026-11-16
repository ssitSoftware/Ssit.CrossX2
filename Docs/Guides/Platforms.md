# Platforms and shaders

CrossX2 renders through the SDL3 GPU API. The GPU driver is chosen from the shader formats the
engine ships for the current OS (`AppRunnerSdl.GetSupportedShaderFormats`):

| Platform        | SDL GPU driver | Shader source                                   | Format passed to SDL |
|-----------------|----------------|-------------------------------------------------|----------------------|
| macOS / iOS     | Metal          | `Assets/Shaders/Metal/**/*.metal`               | MSL (source)         |
| Windows         | Direct3D 12    | `Assets/Shaders/HLSL/**/*.hlsl`                 | DXBC SM 5.1          |

## Windows

HLSL shaders are embedded as source and compiled on first use with `d3dcompiler_47.dll`
(`D3DShaderCompiler`), which is part of every Windows 10/11 install, so no shader toolchain
is needed to build or run. Compiled bytecode is cached for the lifetime of the process.
Compile errors are thrown as `InvalidOperationException` with the compiler output.

The desktop build only targets `net10.0` on Windows. To also build the Android/iOS targets
there (with the workloads installed), pass `-p:CrossXMobileTargets=true`.

The GPU device is created with SDL debug mode first; if that fails (for example when the D3D12
debug layer from the optional "Graphics Tools" Windows feature is not installed) it retries
without debug mode.

## Writing shaders

Each pipeline/effect has a Metal and an HLSL version with the same resource name, e.g.
`Pipelines.TextureLight.frag` maps to `Metal/Pipelines/TextureLight.frag.metal` and
`HLSL/Pipelines/TextureLight.frag.hlsl`. When you change one, change the other.

HLSL rules imposed by SDL GPU on Direct3D 12:

- Vertex attributes use semantic `TEXCOORD{location}` (location as in `SDL_GPUVertexAttribute`).
- Vertex stage: textures/samplers in `space0`, uniform buffers in `register(bN, space1)`.
- Fragment stage: texture/sampler N in `register(tN, space2)` / `register(sN, space2)`,
  uniform buffers in `register(bN, space3)`.
- Entry points are `vertexMain` / `fragmentMain`.
- Uniform matrices come from `System.Numerics.Matrix4x4`; with HLSL's default column-major
  packing use `mul(transform, position)`, matching `transform * position` in Metal.
