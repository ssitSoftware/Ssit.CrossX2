# UI

The CrossX2 UI framework is a retained view tree built for game menus, HUDs and tools. It works with keyboard, gamepad, mouse and touch from the same page definition.

The main pieces are:

- **Views** are plain data objects that describe what to show: `Label`, `Button`, `VerticalStack` and so on.
- **View handlers** are created for each view. They lay it out, draw it and process its input. See [Custom views and handlers](CustomViews.md).
- **Pages** build a view tree for one screen. Each page is paired with a view model.
- **Navigation** creates view models and pages and keeps the back stack.
- **The UI app** is an `IAppComponent` that runs input, navigation, update and drawing.

## Setting Up a UI App

Build the UI app in `IAppInitializer.Initialize` and return it as the root component:

```csharp
using Ssit.CrossX2.Framework.UI;
using Ssit.CrossX2.Framework.UI.Services;

public IAppComponent Initialize(IIoCContainer container, IRenderHostParameters parameters)
{
    parameters.DesignSize = new Size(512, 288);
    parameters.Flags = RenderHostFlags.ExactSize;

    return CrossX2Ui.CreateAppBuilder(container)
        .WithServiceRegistrar(services => services.WithSingleton<IGameSettings, GameSettings>())
        .WithAutoNavigationMapping(typeof(MyAppInitializer).Assembly)
        .WithStyles(typeof(AppStyles))
        .WithBackgroundColor(new RgbaColor(0.05f, 0.05f, 0.1f))
        .WithUiAppInitialization(app => app.Services.Get<IUiSounds>()
            .AddSound(UiSounds.NavigateToSound, "assets:/Sfx/Forward.wav")
            .AddSound(UiSounds.NavigateBackSound, "assets:/Sfx/Back.wav")
            .AddSound(UiSounds.ItemNavigateSound, "assets:/Sfx/Move.wav")
            .AddSound(UiSounds.ExecuteSound, "assets:/Sfx/Select.wav"))
        .WithFirstNavigation<MainMenuViewModel>()
        .Build();
}
```

Builder methods:

| Method | Purpose |
| --- | --- |
| `WithServiceRegistrar` | Registers services in the UI container. It is a child of the app container, so app services stay visible. |
| `WithHandlersMapping` | Maps custom views to handlers (`map.AddMapping<TView, THandler>()`). |
| `WithNavigationMapping` | Maps view models to pages explicitly (`map.Map<TViewModel, TPage>()`). |
| `WithAutoNavigationMapping` | Scans an assembly and maps each non-abstract page to the generic argument of its direct base class, such as `MainMenuPage : Page<MainMenuViewModel>` or `OptionsPage : MenuPage<OptionsViewModel>`. |
| `WithStyles` | Loads global styles from static classes. |
| `WithBackgroundColor` | Clear color for each frame. |
| `WithUiAppInitialization` | Runs after the UI container is built and before the first navigation. |
| `WithFirstNavigation<TViewModel>` | The first page to show. |

Extensions can bundle these calls. For example, the games module provides `builder.WithXxGames()`, which registers its services and the `GameView` handler.

### UI Sounds

`IUiSounds` maps sound ids (constants in `UiSounds`) to sound effects loaded with `AddSound(id, path)`. Missing files are ignored and missing ids return `null`. Handlers check for `null`, but navigation does not: register `NavigateToSound` and `NavigateBackSound` whenever you use `NavigateTo` or `NavigateBack`, or navigation will throw. Other ids are optional: `ItemNavigateSound`, `ExecuteSound`, `ButtonPushSound`, `ButtonReleaseSound`, `ChangeValueSound`, `ChangeOptionSound` and `MenuSound`.

## Pages and View Models

A page derives from `Page<TViewModel>` and builds its view tree in `CreateView`. The view model is a regular class created through the IoC container, so its constructor can take services.

```csharp
using Ssit.CrossX2.Framework.Commands;
using Ssit.CrossX2.Framework.UI;
using Ssit.CrossX2.Framework.UI.Services;
using Ssit.CrossX2.Framework.UI.Views;

public sealed class MainMenuViewModel(INavigation navigation, IAppWindowManager window) : IPageCommandsSource
{
    public ICommand PlayCommand { get; } = new SyncCommand(() => navigation.NavigateTo<GameViewModel>());
    public ICommand OptionsCommand { get; } = new SyncCommand(() => navigation.NavigateTo<OptionsViewModel>());
    public ICommand QuitCommand { get; } = new SyncCommand(window.Close);

    public ICommand BackCommand => QuitCommand;
}

public sealed class MainMenuPage : Page<MainMenuViewModel>
{
    public override float TransitionTime => 0.2f;

    protected override View CreateView() => new VerticalStack
    {
        HorizontalAlign = Align.Center,
        VerticalAlign = Align.Center,
        Spacing = 4,
        Children =
        [
            new LabelButton { Style = "menu", Text = "Play", Command = ViewModel.PlayCommand, UniqueId = NextId() },
            new LabelButton { Style = "menu", Text = "Options", Command = ViewModel.OptionsCommand, UniqueId = NextId() },
            new LabelButton { Style = "menu", Text = "Quit", Command = ViewModel.QuitCommand, UniqueId = NextId() },
        ]
    };
}
```

The page itself is also a `View`, but it is not drawn; only the root returned by `CreateView` is.

Page members you can use or override:

- `ViewModel` holds the view model instance.
- `Services` is the UI IoC container.
- `CreateView()` builds the root view. It is called once when the page loads.
- `OnLoad(IInputContext)` runs after the handlers exist. Use it to set focus.
- `OnUpdate(float dt)` runs every frame. Call `base.OnUpdate(dt)` to keep updating the view tree.
- `OnDraw(IRenderer)` draws the page. Call the base to draw the tree.
- `OnUiButton(UiButton, IInputContext)` handles UI buttons that the focused element did not handle.
- `OnBack()` and `OnMenu()` default to the view model's `BackCommand` and `MenuCommand`.
- `OnTransitionToFinished()` runs when the incoming transition completes.
- `TransitionTime` is the page transition length in seconds (default 0).
- `FocusNavigationMode` and `DefaultFocusId` control focus; see [Focus](#focus).
- `ScreenSize`, `Scale` and `IsInTransition` describe the current page state.

The view model can opt into more behavior:

- `IPageCommandsSource` supplies `BackCommand` and `MenuCommand`.
- `IUiButtonHandler.OnUiButton(UiButton)` sees UI buttons before the page's default handling. Return `true` to consume one.
- `INavigationEventHandler.OnNavigatedBackTo()` is called when the user returns to this view model.
- `IDisposable` view models are disposed after they leave the stack and the transition ends.
- `[NoHistory]` on a view model class removes it from the back stack, so `NavigateBack` skips it.

`MenuPage<TViewModel>` is a base for menu screens. It has a default 0.1 s transition and a `DefaultId`: the first direction or select press focuses that element when nothing is focused yet. `PageWithTranslator<TViewModel>` adds a `Translator` property. It requires `ITranslator`, registered with `services.WithTranslator("assets:/Lang/en.json", ...)` from `Ssit.CrossX2.Framework.UI.Common`.

## Navigation

Inject `INavigation` into view models.

| Method | Behavior |
| --- | --- |
| `NavigateTo<TViewModel>(parameter, skipTransition, navigationSound)` | Pushes a new view model. `parameter` is passed to its constructor. |
| `NavigateBack()` | Pops to the previous page, skipping `[NoHistory]` view models, and restores the element that had focus. |
| `NavigateBackTo<TViewModel>()` | Pops until that view model type is on top. |
| `ClearNavigateTo<TViewModel>(parameter)` | Replaces the whole stack with one page, without a sound. |
| `ClearNavigateToSerie(params Type[])` | Replaces the stack with several view models, showing the last. |
| `IsOnTop(vmOrPage)` | Checks whether a view model or page is current. |
| `NavigationStackCount` | Number of entries in the stack. |
| `ParallelTransitions` | Animates the outgoing and incoming pages together instead of one after the other. |

Parameters are passed into the view model's constructor:

```csharp
navigation.NavigateTo<LevelViewModel>(levelInfo);

public sealed class LevelViewModel(LevelInfo level, INavigation navigation) { /* ... */ }
```

## Layout

Each view has layout properties:

| Property | Meaning |
| --- | --- |
| `Width`, `Height` | Size as a `Length`. Defaults depend on the view; most fill their parent. |
| `HorizontalAlign`, `VerticalAlign` | `Start`, `Center`, `End` or `Fill`. |
| `AnchorX`, `AnchorY` | Explicit position inside a `Container`. Without it, the position follows the alignment. |
| `Visible` | A `SharedBool`. Hidden views are not drawn, receive no pointer input and cannot be focused. |
| `Style` | Style names, separated by spaces or commas. |
| `Transitions` | Per-view transitions played during page navigation. |

### Length

`Length` combines design units, a percentage of the parent, and a fixed number of render pixels: `Value * scale + Percent * parent + Pixels`. Design units are scaled by the render host scale, so layouts written for the design size work at any window size.

```csharp
Width = 120;          // 120 design units
Width = "50%";        // half the parent
Width = "100%-16";    // parent width minus 16 design units
AnchorX = "C";        // 50%; "@" means 100%
Height = "auto";      // size to content
Width = Length.Fill;  // same as "100%"
```

String forms: numbers, `N%`, `C`, `@`, `auto`, `fill` and `zero`, combined with `+` and `-`.

### Thickness

`Thickness` is used for `Padding`. It converts from one length, a `(horizontal, vertical)` pair, or a `(left, top, right, bottom)` tuple:

```csharp
Padding = 4;
Padding = (8, 4);
Padding = (8, 4, 8, 0);
```

### Containers

- `Container` places each child independently using its alignment and anchors. Children can overlap; later children draw on top.
- `VerticalStack` places children top to bottom with `Spacing`. Each child's horizontal alignment applies, and its height is its own height. An auto-height stack sizes itself to its children.
- `HorizontalStack` is the same, left to right.
- `ScrollView` scrolls a single `ContentView`.

All containers have `Padding`, `BackgroundColor`, and a `LayoutSignal` that forces a relayout when triggered.

A view's `RecalculatePositionAndSize()` asks its parent to lay it out again. Use it after changing size-related properties at runtime. Text changes on labels already trigger it.

## Styles

A style is a static method with `[Style("name")]` and one parameter. It runs on each view whose `Style` contains the name and whose type matches the parameter (including subclasses). Styles run when the handler is created, before the handler reads the view, so they act as defaults that later code can still change.

```csharp
public static class AppStyles
{
    [Style("menu")]
    private static void Menu(LabelButton button)
    {
        button.Height = 16;
        button.TextColors = new ButtonStateColors
        {
            Normal = new RgbaColor(0.7f, 0.7f, 0.7f),
            Focused = RgbaColor.White,
            Hover = RgbaColor.White,
            Disabled = new RgbaColor(0.3f, 0.3f, 0.3f),
        };
        button.HapticFeedback = true;
    }

    [Style("__default__")]
    private static void DefaultLabel(Label label) => label.TextColor ??= RgbaColor.White;
}
```

Load global style classes with `WithStyles`. A page can also declare `[Style]` methods on its own class; they apply only to that page, after the global ones. Views with an empty `Style` receive the `__default__` style.

## Values and Binding

View properties that change at runtime use shared value types. The handler subscribes to their change events, so updating the value updates the screen.

- `SharedString` is used for text. `SharedStringValue` is mutable (`SetText`, `FormatText`, `AppendText`); `+` joins strings and stays live. A plain `string` converts implicitly.
- `SharedBool` is used for flags such as `Visible`. A `bool` converts to a constant. `SharedBoolMutable` can change through `SetValue`, and `&`, `|` and `!` build live expressions.
- `SharedValue<T>` is a general mutable value with `ValueChanged`, used by sliders, radio groups and `HapticFeedback`.
- `IValueConsumer<T>` receives values. Focusable views have `FocusConsumer`, which receives `true`/`false` as focus changes.

```csharp
public sealed class HudViewModel
{
    public SharedStringValue Score { get; } = new("0");
    public SharedBoolMutable Paused { get; } = new(false);
}

new Label { Text = "Score: " + ViewModel.Score, Visible = !ViewModel.Paused };
```

## Colors

`ColorWrapper` describes a color that can depend on the render pass and the parent:

```csharp
BackgroundColor = new RgbaColor(0.1f, 0.1f, 0.2f);
BackgroundColor = (RgbaColor.Black, glowColor);   // separate color for the glow pass
TextColor = ("ForegroundColors", RgbaColor.White); // take the parent's color id, with a fallback
```

A string id asks the nearest parent handler that implements `IColorSource` for a color. `Button` is one: its children can use `"ForegroundColors"`, `"OutlineColors"` or `"BackgroundColors"` to follow its hover, focus and press state.

`ButtonStateColors` (an `IButtonStateColors`) picks a color by state: `Normal`, `Hover`, `Focused`, `Pushed`, `Disabled` and `Checked`, with `*Glow` variants for the glow pass.

Each built-in handler draws black in the glow pass unless a glow color is set, so the UI does not glow by default.

## Built-In Views

| View | Purpose |
| --- | --- |
| `Background` | Filled rectangle. Base class for most views. |
| `Frame` | Background with a border (`FrameColor`, `FrameWidth`). |
| `Container`, `VerticalStack`, `HorizontalStack` | Layout containers. |
| `Label` | Text with font, alignment, outline, spacing and padding. Auto-sizes to its text. |
| `BlinkingLabel` | Label that blinks (`VisibleTime`, `HiddenTime`). |
| `MarkdownView` | Formatted text from a Markdown subset. |
| `LabelButton`, `LabelButtonEx` | Text button. The `Ex` variant adds an animated focus bevel. |
| `LabelRadio` | Text button that writes its `Value` into a shared `SelectedValue`. |
| `Button`, `ButtonEx` | Button that contains any views. |
| `IconCheckBox` | Toggle shown with an image. |
| `HorizontalSlider` | Integer slider bound to `SharedValue<int>`. |
| `TextInput` | Editable text with native keyboard support. |
| `FocusableContainer` | Container that can receive focus and forwards UI buttons to its children. |
| `ScrollView` | Scrolls its content with touch or mouse drag, can follow focus, and supports auto-scroll. |
| `ImageView`, `IconView`, `SpriteView`, `NinePatchCard` | Images, icons, animated sprites and nine-patch cards. |
| `VirtualButton` | On-screen gamepad button that writes to `IVirtualGameInput`. |
| `CustomDataView` | Carries arbitrary `Data` for a custom handler. |

`ListBox<TModel>` and `StopwatchControl` exist as views but have no default handler mapping.

## Input in the UI

Each frame the UI app processes queued UI actions, then input, then updates navigation and pages. Input goes through two channels: UI buttons for keyboard and gamepad, and pointers for mouse and touch.

### UI Buttons

Device input is translated into `UiButton` values:

| `UiButton` | Keyboard | Gamepad (player 0) | Virtual input |
| --- | --- | --- | --- |
| `Left`, `Right`, `Up`, `Down` | Arrow keys | D-pad, left stick | D-pad |
| `Select` | Return | A | A |
| `Back` | | B | B |
| `Menu` | | Start | Start |
| `MenuOrBack` | Escape | | |

Directions repeat while held: first after 200 ms, then every 100 ms. With `UiParameters.UseJapaneseUiButtons = true`, A and B are swapped for `Select` and `Back`. Set it from `WithUiAppInitialization` through `app.Services.Get<UiParameters>()`.

A UI button goes to the focused element first (`IFocusable.OnUiButton`), then to the page (`OnUiButton`), which passes it to the view model's `IUiButtonHandler` and finally to `OnBack`/`OnMenu`. Processing stops as soon as something returns `true`, and also when the handler navigates away. Pages ignore UI buttons during transitions.

### Focus

Only handlers that implement `IFocusable` and have a non-empty `UniqueId` can be focused. Set `UniqueId` on buttons, inputs and other focusable views.

- `NextId()` generates a unique id. The first generated id also becomes `DefaultFocusId`, which receives focus when the page loads.
- Arrow keys and the d-pad move focus geometrically: the closest focusable element in that direction, measured from the current position.
- `FocusNavigationMode` sets the allowed directions: `UpDown` (default), `LeftRight`, and `Wrap` to jump to the opposite edge when nothing is found.
- `NavigateBack` restores the element that was focused when you left the page.

The focus highlight is shown only while `PageInputContext.ShowFocus` is true. A first direction press on a hidden highlight shows it without moving focus; mouse or touch input hides it again (unless the pointing mode is `AutoHideMouse`). This keeps menus clean for mouse users and clear for gamepad users.

Code can move focus through `IInputContext`, which is passed to `OnLoad` and to every input callback:

```csharp
protected override void OnLoad(IInputContext inputContext)
{
    var play = inputContext.FindFocusable("play", this);
    inputContext.Focus(play, this);
}
```

### Pointers

Mouse and touch pointers are transformed into page coordinates, then offered to every visible handler that implements `IInputConsumer`. Handlers get them in reverse tree order, so the top-most view (drawn last) sees them first. A handler can capture a pointer with `IInputContext.CapturePointer`; other handlers then get `CancelPointer` and no longer see it. Buttons capture on press, so a press that starts on a button stays with that button even when the pointer moves.

### Commands and Threading

Buttons execute their `ICommand` (from `System.Windows.Input`) on a background task, after `KeyCommandDelay` (keyboard and gamepad only) and `CommandDelay`. The delays give the pressed state and sound time to show before a page change. `IAsyncCommand` is awaited. Until the command finishes, the button shows as pressed and ignores further presses, and while it is the focused element all other input is blocked.

Because commands run off the UI thread, changes that touch views or handlers should go through `IUiActionDispatcher`:

```csharp
public sealed class LoadViewModel
{
    public SharedStringValue Status { get; } = new("");
    public ICommand LoadCommand { get; }

    public LoadViewModel(IUiActionDispatcher dispatcher, ISaveService saves)
    {
        LoadCommand = new AsyncCommand(async () =>
        {
            var summary = await saves.LoadSummaryAsync();
            dispatcher.Enqueue(() => Status.SetText(summary));
        });
    }
}
```

`CanExecute` controls the enabled state. Call `RaiseCanExecuteChanged()` on `SyncCommand` or `AsyncCommand` when it changes.

`EnabledCommandTypes` lets left and right execute the command, which suits option selectors. With `Next` or `Previous` enabled, the command parameter becomes a `(CommandParameter, ButtonCommandType)` tuple.

## Transitions

Page transitions use `TransitionTime` and a progress value from the navigation. Views opt in with `Transitions`; each one applies only to the transition types in `ForTransitions`:

```csharp
Transitions =
[
    new FadeTransition { ForTransitions = TransitionType.Navigation },
    new TranslationTransition { ForTransitions = TransitionType.NavigateTo, Offset = new Vector2(0, 16) },
]
```

`Power`, `ProgressMin` and `ProgressMax` shape the curve. Implement `ITransition` or derive from `Transition` for custom effects; a transition changes render state in `Apply` and the base class restores it after the view draws.

## Rendering Notes

The UI draws in render target pixels. Handler `ScreenBounds` are already scaled, so 1 design unit equals `Scale` pixels. Text uses the `Default` font family at size 12 unless a view sets `Font`. With `TextScaling.Default` text is rendered at the scaled size for sharp results; `TextScaling.Pixel` renders at design size and scales the pixels up, which keeps pixel fonts crisp.

When the glow pass is enabled, the whole UI is drawn twice. See [Rendering](Rendering.md).
