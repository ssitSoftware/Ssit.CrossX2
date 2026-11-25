using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.UI.Views;

/// <summary>
/// Wrapped text (<see cref="Text"/>) typed letter by letter: the whole <see cref="Label.Text"/> is laid out once (when it
/// changes) and only its first <see cref="VisibleCharacters"/> characters are drawn - in the places they have in the
/// whole text, so the words never jump to another line and the justified lines keep their spacing while typing.
/// A blinking <see cref="CursorCharacter"/> is drawn after the last visible character.
/// </summary>
public class CutsceneText : Text
{
    /// <summary>
    /// Number of the first characters of the text to draw (counted in the whole text, line breaks included).
    /// All characters when null.
    /// </summary>
    public SharedValue<int> VisibleCharacters { get; set; }

    /// <summary>
    /// Character drawn after the last visible character; '\0' = no cursor.
    /// </summary>
    public char CursorCharacter { get; set; } = '_';

    /// <summary>
    /// How long (seconds) the cursor is shown and then hidden while blinking. It stays shown while the visible characters
    /// change (typing) and starts blinking when they stop.
    /// </summary>
    public float CursorBlinkTime { get; set; } = 0.5f;

    /// <summary>
    /// Opacity of the text, its outline and the cursor (0 - 1). Null = opaque.
    /// </summary>
    public SharedValue<float> Opacity { get; set; }
}
