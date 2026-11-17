namespace Ssit.CrossX2.Framework.UI.Views;

/// <summary>
/// Multiline text which wraps at word boundaries to the width of the view (minus padding).
/// Give it a fixed or fill width; an auto height follows the wrapped lines. Explicit line breaks ('\n') are kept.
/// <para>
/// Note: inside the Ssit.CrossX2.Framework namespaces the name collides with the Ssit.CrossX2.Framework.Text
/// namespace - refer to it as <c>Views.Text</c> there.
/// </para>
/// </summary>
public class Text : Label
{
    /// <summary>
    /// Distance between the lines as a multiple of the font's line height (1 = lines touching, 1.5 = half a line
    /// of space between them). Added on top of <see cref="Label.LineSpacing"/>.
    /// </summary>
    public float Interline { get; set; } = 1;
}
