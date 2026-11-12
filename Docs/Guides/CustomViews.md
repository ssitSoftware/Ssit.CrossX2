# Custom Views and Handlers

Every UI element is two classes:

- A **view** is a plain data object. It has public, settable properties and no behavior. Pages create views in `CreateView`, and styles set their defaults.
- A **view handler** is created by the framework for each view instance. It lays out, draws, updates and handles input for that view. Handlers are constructed through the IoC container, so they can inject any service.

This split keeps pages declarative: a page describes *what* is on screen, and the handler owns *how* it works. This guide shows how to write both halves.

## A Display-Only View

Start with the view. Derive from `View`, or from `Background` to get a `BackgroundColor` for free:

```csharp
using Ssit.CrossX2.Framework.UI.Values;
using Ssit.CrossX2.Framework.UI.Views;

public class ProgressBar : Background
{
    public SharedValue<float> Progress { get; set; } = 0f;   // 0..1
    public ColorWrapper BarColor { get; set; }
    public ColorWrapper? FrameColor { get; set; }
}
```

Use shared value types (`SharedValue<T>`, `SharedBool`, `SharedString`) for anything that changes while the page is shown, so the view model can update it without touching the handler. Plain types are fine for configuration set once.

Then the handler. Derive from `ViewHandler<TView>`, or from the handler that matches your view's base class (`BackgroundHandler<TView>` here). The first constructor parameter is always `CreateHandlerParameters`; anything after it is resolved from the container.

```csharp
using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.UI.Handlers;

public class ProgressBarHandler<TView>(ViewHandler.CreateHandlerParameters parameters)
    : BackgroundHandler<TView>(parameters) where TView : ProgressBar
{
    private readonly IColorSource _colorSource = parameters.Parent?.GetParent<IColorSource>(true);

    protected override void OnDraw(IRenderer renderer)
    {
        base.OnDraw(renderer); // background

        var progress = Math.Clamp(AttachedView.Progress?.Value ?? 0f, 0f, 1f);
        var bar = new RectangleF(ScreenBounds.X, ScreenBounds.Y, ScreenBounds.Width * progress, ScreenBounds.Height);

        var barColor = AttachedView.BarColor.GetColor(renderer, _colorSource);
        if (barColor.HasValue)
        {
            renderer.GeometryRenderer.FillRectangle(bar, barColor.Value);
        }

        var frameColor = AttachedView.FrameColor?.GetColor(renderer, _colorSource);
        if (frameColor.HasValue)
        {
            renderer.GeometryRenderer.DrawRectangle(ScreenBounds, frameColor.Value);
        }
    }
}

public class ProgressBarHandler(ViewHandler.CreateHandlerParameters parameters)
    : ProgressBarHandler<ProgressBar>(parameters);
```

The built-in handlers follow this pattern: a generic handler constrained to the view type, plus a closed, non-generic class for the default mapping. The generic version lets you subclass both the view and the handler later.

Register the mapping when building the UI app:

```csharp
CrossX2Ui.CreateAppBuilder(container)
    .WithHandlersMapping(map => map.AddMapping<ProgressBar, ProgressBarHandler>())
    // ...
```

Then use the view in a page:

```csharp
new ProgressBar
{
    Height = 6,
    Progress = ViewModel.LoadProgress,
    BarColor = RgbaColor.White,
    BackgroundColor = new RgbaColor(0.2f, 0.2f, 0.2f),
}
```

### Handler Lookup

Handlers are found by the view's exact type. If you derive `BossHealthBar : ProgressBar`, map `BossHealthBar` too; the mapping for `ProgressBar` is not used for subclasses.

A single view instance can use a different handler through `CustomHandlerType`. The type must be a handler for that view type. `CustomHandlerParameters` is passed to the handler as `parameters.AdditionalParameters`:

```csharp
new ProgressBar
{
    CustomHandlerType = typeof(SegmentedProgressBarHandler),
    CustomHandlerParameters = 10, // segment count
}
```

## Handler Lifecycle

| Step | Called when | Use it for |
| --- | --- | --- |
| Constructor | The handler is created. Styles have already been applied to the view. | Injecting services, subscribing to view events, creating child handlers. |
| `Init()` | Right after construction. | Work that needs the handler fully constructed, such as measuring text. |
| `SetBounds(RectangleF)` | The parent lays the view out. | Caching layout-dependent data. Call the base first. |
| `Update(float dt)` | Every frame, and again with `dt == 0` while layout settles. | Animation and state. Ignore time-based work when `dt` is 0. |
| `Draw(IRenderer)` | The parent draws. Calls `OnDraw` only when visible, with view transitions applied. | Override `OnDraw`, not `Draw`. |
| `Dispose()` | The page is disposed, or the view is removed from its container. | Override `OnDispose(bool disposing)` and unsubscribe from events. |

Useful members:

- `AttachedView` is the view, typed as `TView`.
- `Parent` is the `IViewParent`. `Parent.GetParent<T>(optional: true)` walks up the tree to find a container, color source or the page.
- `Bounds` is the rectangle relative to the parent's content area; `ScreenBounds` is the absolute rectangle in render target pixels. Draw and hit-test with `ScreenBounds`.
- `CurrentScale` is the number of pixels per design unit. Multiply design-unit sizes by it when drawing.

Unsubscribe from everything you subscribe to. Pages are created and disposed on every navigation, and view models usually outlive them.

```csharp
public ProgressBarHandler(CreateHandlerParameters parameters) : base(parameters)
{
    AttachedView.Progress.ValueChanged += OnProgressChanged;
}

protected override void OnDispose(bool disposing)
{
    base.OnDispose(disposing);
    AttachedView.Progress.ValueChanged -= OnProgressChanged;
}
```

Shared value events fire on the thread that changed the value. Button commands run on a background task, so a change made from a command can reach your handler off the UI thread. Keep event handlers small: set a flag and react in `Update`, or queue the work with `IUiActionDispatcher.Enqueue`.

## Size and Layout

The parent asks each child for its size and alignment, then calls `SetBounds`. The defaults read `View.Width`/`View.Height` (falling back to `Length.Fill`) and `View.HorizontalAlign`/`View.VerticalAlign` (falling back to `Align.Fill`).

Override `CalculateSize` when the view has a natural content size, so `"auto"` and stack layouts work:

```csharp
public override void CalculateSize(out Length width, out Length height)
{
    width = View.Width ?? Length.Auto;
    height = View.Height ?? Length.Auto;

    if (width.IsAuto) width = _iconSize.Width;    // design units
    if (height.IsAuto) height = _iconSize.Height;
}

public override void CalculateAlign(out Align horizontal, out Align vertical)
{
    horizontal = View.HorizontalAlign ?? Align.Center;
    vertical = View.VerticalAlign ?? Align.Center;
}
```

Return `Length` values in design units (a `float` converts implicitly). If the content size is measured in pixels, divide by `CurrentScale`.

When the natural size changes at runtime, ask the parent to lay the view out again with `Parent?.RecalculateLayout(AttachedView)` from the handler, or `View.RecalculatePositionAndSize()` from outside. The layout pass runs before the next draw.

## Drawing

- Draw inside `ScreenBounds`. Scale design-unit measurements with `CurrentScale`.
- Resolve colors through `ColorWrapper.GetColor(renderer, colorSource)`. It handles glow colors and parent color ids for you.
- When the glow pass is enabled, `OnDraw` runs twice. Check `renderer.CurrentPass == RenderPass.Glow` and draw black (or a glow color) for parts that should not glow. Skipping drawing in the glow pass lets glowing content behind the view show through.
- Wrap state changes in `renderer.StateManager.SaveState()` and `RestoreState()`, for example for clipping with `SetClipRect` or a temporary `Translate`.

## Handling Input

Handlers take part in input by implementing interfaces. The input processor finds them by walking the handler tree, so a handler only needs to implement the interface; no registration is needed.

### Pointers: IInputConsumer

```csharp
public interface IInputConsumer
{
    void ProcessHover(Vector2? hoverPosition, int? matchingPointerId, IInputContext context);
    bool ProcessInput(IReadOnlyList<Pointer> pointers, IInputContext context);
    void CancelPointer(int pointerId, IInputContext context);
}
```

- Pointer positions are already in the same space as `ScreenBounds`, so `ScreenBounds.Contains(pointer.Position)` is a valid hit test.
- `ProcessHover` receives the mouse position, or `null` when there is none. `matchingPointerId` is set when a pressed pointer is at the hover position.
- Handlers are called top-most first. To claim a pointer, call `context.CapturePointer(pointer.Id, this)` and return `true`. The processor removes captured pointers from the list before the next handler and calls `CancelPointer` on every other handler.
- Track the pointer you own by `Id` until it reports `JustReleased` or disappears from the list. Treat `CancelPointer` for that id as "drop it, do nothing".

### Keyboard and Gamepad: IFocusable

Keyboard and gamepad input reaches handlers through focus. A focusable handler receives `UiButton` values (`Left`, `Right`, `Up`, `Down`, `Select`, `Back`, `Menu`, `MenuOrBack`) while it has focus.

```csharp
public interface IFocusable
{
    bool Enabled { get; }
    bool Focused { get; }
    RectangleF ScreenBounds { get; }   // already provided by ViewHandler
    bool DisableAllInput { get; }
    bool OnUiButton(UiButton button, IInputContext inputContext);
    void SetFocus();
    bool ResetFocus();
    string UniqueId { get; }
    bool SkipNavigation { get; }
}
```

Rules to follow:

- **Expose an id.** Only handlers with a non-empty `UniqueId` can receive focus. Let the view implement `IFocusableView` (`UniqueId`, `FocusConsumer`) and return `AttachedView.UniqueId`.
- **Move focus yourself.** The page does not move focus on its own. When your handler doesn't use a direction, call `context.MoveFocus(direction, this)`. Otherwise users cannot leave your view with the d-pad.
- **Return `true` only when you consumed the button.** Unhandled buttons, `Back` in particular, then go to the page.
- **Report focus.** Set `Focused` in `SetFocus`/`ResetFocus` and pass it to `AttachedView.FocusConsumer?.SetValue(...)`. Return `false` from `ResetFocus` to keep focus, for example while a text field is being edited.
- **Show the focus highlight only when wanted.** Inject `PageInputContext` and draw the focused state only while `ShowFocus` is true.
- `SkipNavigation = true` keeps the element out of d-pad navigation while still allowing code to focus it. `DisableAllInput = true` while focused blocks all other input, which suits modal states.

### Children Inside Buttons: IUiCommandHandler

`Button` and `FocusableContainer` own focus for their whole area. Before handling a UI button themselves, they pass it to child handlers that implement `IUiCommandHandler`. That is how a slider inside a focusable row reacts to left and right. Implement `IUiCommandHandler` on handlers meant to sit inside those containers.

### Reusing Button Behavior

For anything that acts like a button, reuse `ButtonHelper<TView, THandler>` from `Ssit.CrossX2.Framework.UI.Handlers.Helpers`. It implements press and hover tracking, pointer capture, focus movement, sounds, haptics, delayed command execution and the `ShowFocus` rules. The view must implement `IButtonView` and the handler must implement `IFocusable` and `IInputConsumer`; forward the interface calls to the helper exactly as `LabelButtonHandler` does, and dispose the helper in `OnDispose`.

## Example: A Focusable Toggle

The following toggle works with mouse, touch, keyboard and gamepad.

```csharp
using System.Numerics;
using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Input;
using Ssit.CrossX2.Framework.UI.Common.Pages;
using Ssit.CrossX2.Framework.UI.Handlers;
using Ssit.CrossX2.Framework.UI.Parameters;
using Ssit.CrossX2.Framework.UI.Services;
using Ssit.CrossX2.Framework.UI.Values;
using Ssit.CrossX2.Framework.UI.Views;

public class Toggle : Background, IFocusableView
{
    public SharedValue<bool> IsOn { get; set; } = false;
    public ColorWrapper OnColor { get; set; } = RgbaColor.White;
    public ColorWrapper OffColor { get; set; } = new RgbaColor(0.3f, 0.3f, 0.3f);
    public ColorWrapper FocusColor { get; set; } = RgbaColor.White;

    public string UniqueId { get; set; }
    public IValueConsumer<bool> FocusConsumer { get; set; }
}

public class ToggleHandler(
    ViewHandler.CreateHandlerParameters parameters,
    PageInputContext pageInputContext,
    IUiSounds sounds)
    : BackgroundHandler<Toggle>(parameters), IInputConsumer, IFocusable
{
    private int? _pointerId;

    public bool Enabled => true;
    public bool Focused { get; private set; }
    public bool DisableAllInput => false;
    public bool SkipNavigation => false;
    public string UniqueId => AttachedView.UniqueId;

    public override void CalculateSize(out Length width, out Length height)
    {
        width = View.Width ?? 24f;
        height = View.Height ?? 12f;
    }

    private void Flip(bool? value = null)
    {
        var isOn = AttachedView.IsOn;
        var newValue = value ?? !isOn.Value;
        if (newValue == isOn.Value) return;

        isOn.Value = newValue;
        sounds[UiSounds.ChangeValueSound]?.PlayOnce();
    }

    // Pointer input

    public void ProcessHover(Vector2? hoverPosition, int? matchingPointerId, IInputContext context)
    {
    }

    public bool ProcessInput(IReadOnlyList<Pointer> pointers, IInputContext context)
    {
        foreach (var pointer in pointers)
        {
            if (_pointerId is null && pointer.State == ButtonState.JustPressed && ScreenBounds.Contains(pointer.Position))
            {
                _pointerId = pointer.Id;
                context.Focus(this, this);
                pageInputContext.ShowFocus = false;
                context.CapturePointer(pointer.Id, this);
                return true;
            }

            if (pointer.Id == _pointerId && pointer.State == ButtonState.JustReleased)
            {
                _pointerId = null;
                if (ScreenBounds.Contains(pointer.Position))
                {
                    Flip();
                }
                return true;
            }
        }
        return false;
    }

    public void CancelPointer(int pointerId, IInputContext context)
    {
        if (pointerId == _pointerId)
        {
            _pointerId = null;
        }
    }

    // Keyboard and gamepad input

    public bool OnUiButton(UiButton button, IInputContext context)
    {
        if (!pageInputContext.ShowFocus && button is not (UiButton.Back or UiButton.Menu or UiButton.MenuOrBack))
        {
            pageInputContext.ShowFocus = true;   // first press only reveals the highlight
            return true;
        }

        switch (button)
        {
            case UiButton.Select:
                Flip();
                return true;
            case UiButton.Left:
                Flip(false);
                return true;
            case UiButton.Right:
                Flip(true);
                return true;
            case UiButton.Up:
                return context.MoveFocus(FocusDirection.Up, this);
            case UiButton.Down:
                return context.MoveFocus(FocusDirection.Down, this);
            default:
                return false;   // Back and Menu go to the page
        }
    }

    public void SetFocus()
    {
        Focused = true;
        AttachedView.FocusConsumer?.SetValue(true);
    }

    public bool ResetFocus()
    {
        Focused = false;
        AttachedView.FocusConsumer?.SetValue(false);
        return true;
    }

    // Drawing

    protected override void OnDraw(IRenderer renderer)
    {
        base.OnDraw(renderer);

        var isOn = AttachedView.IsOn.Value;
        var knobSize = ScreenBounds.Height;
        var knobX = isOn ? ScreenBounds.Right - knobSize : ScreenBounds.X;
        var knob = new RectangleF(knobX, ScreenBounds.Y, knobSize, knobSize);

        var color = (isOn ? AttachedView.OnColor : AttachedView.OffColor).GetColor(renderer);
        if (color.HasValue)
        {
            renderer.GeometryRenderer.FillRectangle(knob, color.Value);
        }

        if (Focused && pageInputContext.ShowFocus)
        {
            var focusColor = AttachedView.FocusColor.GetColor(renderer);
            if (focusColor.HasValue)
            {
                renderer.GeometryRenderer.DrawRectangle(ScreenBounds, focusColor.Value);
            }
        }
    }
}
```

Register and use it:

```csharp
builder.WithHandlersMapping(map => map.AddMapping<Toggle, ToggleHandler>());

new HorizontalStack
{
    Spacing = 8,
    Children =
    [
        new Label { Text = "Music", Width = 80 },
        new Toggle { IsOn = ViewModel.MusicEnabled, UniqueId = NextId() },
    ]
}
```

`ViewModel.MusicEnabled` is a `SharedValue<bool>` that the view model listens to through `ValueChanged`.

## Container Handlers

To lay out children, derive the view from `ChildrenContainer` and the handler from one of:

- `ContainerHandler<TView>`, which keeps the free layout (alignment and anchors) and lets you add drawing or input on top. `ButtonHandler` is built this way.
- `ChildrenContainerHandler<TView>`, which creates child handlers, draws and updates them, and leaves positioning to you. Implement `RecalculateChildrenLayouts()`. Return early when the protected `RecalculateChildren` set (the views that need layout) is empty. Otherwise position the affected children, or all of them for flow layouts like stacks, then clear the set.

Position children through the protected `Children` list, which holds their handlers in the order they were created; each handler's `View` gives its view. Ask a child for its size and alignment with `CalculateSize` and `CalculateAlign`, then call its `SetBounds` with a rectangle inside `CalculateTargetBounds(Bounds)`, the container's area minus `Padding`.

```csharp
protected override void RecalculateChildrenLayouts()
{
    if (RecalculateChildren.Count == 0) return;

    var area = CalculateTargetBounds(Bounds);
    var columns = 3;
    var cellWidth = area.Width / columns;
    var cellHeight = (AttachedView.CellHeight ?? 24f).Calculate(CurrentScale, area.Height);

    for (var idx = 0; idx < Children.Count; idx++)
    {
        var x = area.X + idx % columns * cellWidth;
        var y = area.Y + idx / columns * cellHeight;
        Children[idx].SetBounds(new RectangleF(x, y, cellWidth, cellHeight));
    }

    RecalculateChildren.Clear();
}
```

Here `CellHeight` is a `Length?` property on the grid view.

A handler that hosts a child outside `Children` (as `ScrollView` does with `ContentView`) must create it with `IHandlerMapper.Create(view, this)`, implement `IViewParent` for it, and implement `IChildrenContainer` so focus search and pointer input can reach it. It must also update, draw and dispose the child itself.

## Reading Devices Directly

A handler can inject `IKeyboard`, `IGameControllers` or `IInputMappings` and read them in `Update`. That is right for game-content views like `GameView`, where the view hosts gameplay. For menus and controls, use `UiButton` and pointers so focus, repeat timing, button swapping and touch all keep working.

## Checklist

- The view holds data only; runtime-changing values are shared types.
- The handler's first constructor parameter is `CreateHandlerParameters`.
- The mapping is registered with `WithHandlersMapping`, for each concrete view type.
- `OnDraw` draws inside `ScreenBounds`, scales by `CurrentScale` and handles the glow pass.
- `CalculateSize` returns design units and supports `auto` where it makes sense.
- Events subscribed in the constructor are removed in `OnDispose`.
- Focusable handlers have a `UniqueId`, move focus for unused directions, and respect `PageInputContext.ShowFocus`.
- Pointer handlers hit-test with `ScreenBounds`, capture the pointers they own and handle `CancelPointer`.
