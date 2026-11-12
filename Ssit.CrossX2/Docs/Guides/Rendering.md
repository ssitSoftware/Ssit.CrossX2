# Rendering

CrossX2 rendering is organized around a render host, one main renderer, specialized renderers, and render state.

## Render Host

The render host owns the logical render target and presentation scaling. Configure it through `IRenderHostParameters` in `IAppInitializer.Initialize`.

```csharp
parameters.DesignSize = new Size(512, 288);
parameters.MinScale = 1;
parameters.MaxScale = 8;
parameters.Flags = RenderHostFlags.ExactSize | RenderHostFlags.EnableGlowPass;
```

Important flags:

- `MatchWidth` scales to match window width.
- `MatchHeight` scales to match window height.
- `ExactSize` preserves the design aspect ratio.
- `PixelPerfect` uses integer scale and point filtering.
- `EnableGlowPass` renders an additional glow pass.
- `EnableCrtSimulation` applies CRT simulation when presenting.

`IRenderHost` exposes the render transform, inverse transform, target size, logical size, and scale.

## Frame Rendering

The backend renders in this order:

1. Submit any previous GPU work.
2. Acquire the swapchain texture.
3. Start the normal render pass.
4. Call `IAppComponent.Draw`.
5. If glow is enabled, start the glow pass and call `Draw` again with `renderer.CurrentPass == RenderPass.Glow`.
6. Composite the frame through the render host.
7. Run optional post rendering.
8. Submit GPU work.

Objects that draw differently in the glow pass should check `IRenderer.CurrentPass`.

## Renderer API

`IRenderer` is the main drawing entry point:

- `StateManager` changes render state.
- `RenderStateProvider` reads render state.
- `GeometryRenderer` draws shapes and primitives.
- `SpriteRenderer` draws textures and sprites.
- `TextRenderer` draws text.
- `RenderQueue` supports deferred ordered drawing.
- `LightingManager` controls ambient and dynamic lights.

Example:

```csharp
public void Draw(IRenderer renderer, ITexture texture)
{
    renderer.StateManager.SetTextureFilter(TextureFilter.Point);
    renderer.SpriteRenderer.Draw(texture, new RectangleF(0, 0, 32, 32));
}
```

## Render State

Render state includes texture filtering, blending, clipping, tinting, and transforms. Reset or set state explicitly when drawing independent systems so rendering behavior does not leak between callers.

## Lighting and Glow

Textures can include diffuse, glow, and light maps. The render host can perform a glow pass when `RenderHostFlags.EnableGlowPass` is enabled. During UI rendering or other unlit drawing, disable lighting through `renderer.LightingManager.EnableLighting(false)` and restore it when needed.

## Post Rendering

`IPostRenderer` draws after the game image has been composed to the screen area. It receives the target viewport where the game image was drawn. Use it for overlays or effects that need screen-space coordinates.
