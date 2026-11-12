# Input

CrossX2 exposes raw device state through small polling services and adds a named mapping layer on top for gameplay code. All input services are registered by the SDL backend, so applications only inject them.

| Service | Purpose |
| --- | --- |
| `IKeyboard` | Keyboard keys and mouse buttons as keys. |
| `IGameControllers` | Gamepad buttons, axes, connection state and rumble per player. |
| `IPointingDevices` | Mouse and touch pointers, hover position, coordinate transforms. |
| `IInputMappings` | Named buttons and axes per player, combining keyboard, gamepad and virtual input. |
| `IVirtualGameInput` | Programmatic gamepad state, used by on-screen controls. |
| `IHapticDevice` | UI and force feedback. |
| `INativeTextInputService` | Platform text entry sessions and on-screen keyboards. |

## Frame Timing

Input is polled once per frame, before any update callback runs. During a frame the SDL runner:

1. Processes SDL events, including gamepad connection and key events.
2. Updates pointing devices and the keyboard snapshot.
3. Raises `IEventSource.Updating`, calls `IUpdatable.Update`, then `IAppComponent.Update`.

Every query made during the same frame returns the same state. Read input in `Update`, not in `Draw`.

## ButtonState

Every button-like query returns a `ButtonState`. It holds two flags: `IsDown` (pressed now) and `IsChanged` (changed since the previous frame).

| State | `IsDown` | `IsChanged` | Meaning |
| --- | --- | --- | --- |
| `ButtonState.Empty` | false | false | Up, unchanged. |
| `ButtonState.JustPressed` | true | true | Pressed this frame. |
| `ButtonState.Down` | true | false | Held. |
| `ButtonState.JustReleased` | false | true | Released this frame. |

`WasDown` gives the previous frame's state. Compare against the static values to detect edges:

```csharp
if (keyboard.GetKey(Key.Space) == ButtonState.JustPressed)
{
    Jump();
}

if (keyboard.GetKey(Key.LeftShift).IsDown)
{
    speed *= 2;
}
```

States can be combined. `|` merges two sources into one logical button: the result is down when either is down and reports a press only when neither was already held. `&` requires both to be down. `WithModifier` is for chords: it reports a press only when the main button itself was just pressed while the modifier is held.

```csharp
var confirm = keyboard.GetKey(Key.Return) | controllers.GetButton(0, GameControllerButton.A);

var save = keyboard.GetKey(Key.S).WithModifier(keyboard.GetKey(Key.LeftCtrl));
if (save == ButtonState.JustPressed)
{
    Save();
}
```

## Keyboard

`IKeyboard.GetKey(Key key)` returns the state of a key. `Key` values are SDL scancodes, so they describe physical key positions rather than the characters produced by the current layout. Use the text input service when you need typed characters.

Mouse buttons are also available as keys through `Key.MouseButtonLeft`, `Key.MouseButtonRight` and `Key.MouseButtonMiddle`. That is convenient for simple "click to shoot" bindings, while `IPointingDevices` is the right choice for anything position-based.

`Key.None` always returns `ButtonState.Empty`, so it can be used as "unbound".

### Key Events

When the root `IAppComponent` also implements `IKeyboardEventHandler`, the runner calls `OnKeyDown(Key)` and `OnKeyUp(Key)` for every SDL key event, including OS key repeat. Both methods have empty default implementations, so implement only what you need. Prefer polling for gameplay and use events for things like debug consoles that need each repeat.

## Game Controllers

`IGameControllers` is indexed by player number, starting at 0.

```csharp
if (controllers.IsConnected(0))
{
    var moveX = controllers.GetAxis(0, GameControllerAxis.LeftX);
    var fire = controllers.GetButton(0, GameControllerButton.RightShoulder);
}
```

Axes return values from -1 to 1 (triggers from 0 to 1). Besides physical buttons, `GameControllerButton` includes digital versions of analog inputs: `LeftStickUp`, `LeftStickDown`, `LeftStickLeft`, `LeftStickRight`, the same for the right stick, plus `LeftTrigger` and `RightTrigger`. These make menu navigation with a stick work like a d-pad.

`GameControllerAxis` also contains `DPadX` and `DPadY` for treating the d-pad as an axis.

### Face Button Layout

`SwitchButtons(player, true)` swaps A with B and X with Y for that player. Use it for controllers or regions where confirm and cancel are reversed. The UI has its own switch for this; see `UiParameters.UseJapaneseUiButtons` in the [UI guide](UI.md).

### Rumble

```csharp
controllers.Vibrate(player: 0, low: Vibration.Level4, high: Vibration.Level2, ms: 150);
```

`Vibration` has levels from `Zero` to `Level10`. The final strength is scaled by `VibrationForce` (default 3). Setting `VibrationForce = 0` turns rumble off globally, which is a good target for an options menu.

## Input Mappings

Gameplay code should rarely read devices directly. `IInputMappings` gives each player a set of named buttons and axes, so code asks for `"Jump"` instead of a specific key, and bindings can change without touching gameplay.

Define mappings once, for example in a startup service or after loading settings:

```csharp
inputMappings.Mapper(0)
    .Clear()
    .MapAxis("MoveX", GameControllerAxis.LeftX)
    .MapAxis("MoveX", Key.A, Key.D)
    .MapAxis("MoveX", GameControllerButton.DPadLeft, GameControllerButton.DPadRight)
    .MapButton("Jump", GameControllerButton.A)
    .MapButton("Jump", Key.Space, alt: Key.W)
    .MapButton("Fire", Key.LeftCtrl, alt: Key.MouseButtonLeft);
```

Then read by name:

```csharp
var mapping = inputMappings[0];

var moveX = mapping.GetAxis("MoveX");
if (mapping.GetButton("Jump") == ButtonState.JustPressed)
{
    Jump();
}

mapping.Feedback(Vibration.Level3, Vibration.Level3, 100);
```

How sources combine:

- One name can have one mapping of each kind: a controller axis, a button pair, a key pair for axes; a controller button (plus alternative) and a key (plus alternative) for buttons. Mapping the same kind again replaces it, which is why the example uses both `key` and `alt`.
- For buttons, all mapped sources are merged, so either the key or the pad button triggers it.
- For axes, all sources are summed and clamped to -1..1. Pressing the negative key gives -1, the positive key +1.
- Player 0 also reads `IVirtualGameInput`, so on-screen controls feed the same names.

`inputMappings[player]` throws `InvalidOperationException` if `Mapper(player)` was never called for that player. Create the mapping before reading it.

### Mapping Descriptors

`Ssit.CrossX2.Framework.Input.InputConfig` contains JSON-friendly descriptors (`MappingDesc`, `AxisDesc`, `ButtonDesc`) for storing bindings in files. The framework does not load them automatically; apply a loaded descriptor through the mapper:

```csharp
var mapper = inputMappings.Mapper(0).Clear();

foreach (var axis in desc.Axes ?? [])
{
    mapper.MapAxis(axis.Id, axis.Axis);
    if (axis.Buttons is not null) mapper.MapAxis(axis.Id, axis.Buttons.Negative, axis.Buttons.Positive);
    if (axis.Keys is not null) mapper.MapAxis(axis.Id, axis.Keys.Negative, axis.Keys.Positive);
}

foreach (var button in desc.Buttons ?? [])
{
    mapper.MapButton(button.Id, button.Button, button.AltButton);
    mapper.MapButton(button.Id, button.Key, button.AltKey);
}
```

## Virtual Game Input

`IVirtualGameInput` is an in-memory gamepad. Anything can write to it, and both the input mappings (player 0) and the UI read it.

```csharp
virtualInput.SetButton(GameControllerButton.A, ButtonState.JustPressed);
virtualInput.SetAxis(GameControllerAxis.LeftX, -1f);
```

The writer owns the state: values stay until they are set again, so set `JustPressed`, then `Down`, then `JustReleased`, then `Empty` across frames. The UI `VirtualButton` view does this for you and is the normal way to build touch controls.

## Pointing Devices

`IPointingDevices` tracks mouse buttons and touches as `Pointer` objects.

```csharp
foreach (var pointer in pointingDevices.Pointers)
{
    if (pointer.State == ButtonState.JustPressed)
    {
        StartDrag(pointer.Id, pointer.Position);
    }
}
```

A `Pointer` has:

- `Id`: `MouseButtons.Left` (1), `MouseButtons.Right` (2), `MouseButtons.Middle` (3) for the mouse; touches get ids above 3 that stay stable for the life of the touch.
- `State`: a `ButtonState`. A pointer appears with `JustPressed`, stays `Down` while held, reports `JustReleased` on release, then `Empty` for one frame before it is removed.
- `Position`: current position in the active coordinate space.
- `Origin`: where the press started, in the same space.
- `OriginalPosition`: the untransformed window position.

`HoverPosition` is the mouse position when no button is pressed, or `null` when the mouse is outside the window or hidden.

### Modes

`Mode` is a `PointingDevicesMode` flag set (in `Ssit.CrossX2.Framework.Input.Internal`):

- `Mouse` enables mouse pointers and hover.
- `Touch` enables touch. It is ignored on platforms other than Android and iOS.
- `AutoHideMouse` hides the cursor and disables mouse input after 3 seconds without movement, then turns it back on when the mouse moves. This suits games that mix gamepad and mouse.
- `Disabled` ignores all pointers.

`LockMouseInWindow` grabs the mouse inside the window. `ShowHoverPointer` controls whether the OS cursor stays visible while hovering.

### Coordinate Transforms

Positions start in window pixels. `PushTransform(Matrix3x2)` transforms every pointer and the hover position; `PopTransform()` restores them. Pushes nest, so always pair them:

```csharp
pointingDevices.PushTransform(renderHost.TransformInv);
try
{
    // Pointer positions are now in render-target space.
}
finally
{
    pointingDevices.PopTransform();
}
```

The UI does exactly this before giving pointers to view handlers, so handlers receive positions in the same space as their `ScreenBounds`.

## Haptics

`IHapticDevice.Feedback(FeedbackStyle style, Vector2? position = null)` plays device haptics such as `ButtonPush`, `ButtonRelease`, `ControlChangeValue` and the `Rumble*` styles. `UiFeedbackLevel` and `ForceFeedbackLevel` (`Disabled` to `Level3`) let users tune or disable each category. UI buttons call this automatically when their `HapticFeedback` value is true.

## Native Text Input

`INativeTextInputService` opens platform text entry, including on-screen keyboards on mobile. A consumer implements `INativeTextInputConsumer`:

```csharp
INativeTextInput session = textInputService.AllocateTextInput(this, InputType.Text);
session.UpdatePosition(fieldBounds, cursorPosition);

// Consumer callbacks:
// OnTextInput(string text)  - characters typed or pasted
// OnKey(Key key)            - editing keys; return true when handled
// OnTextInputClosed()       - the platform closed the session

session.Dispose(); // close the session
```

`InputType` (`Text`, `Password`, `Number`, `Email`) selects the keyboard layout on mobile. In UI code, use the `TextInput` view instead; its handler manages the session, cursor, selection and focus.
