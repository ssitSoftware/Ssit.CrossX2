using Ssit.CrossX2.Text;

namespace Ssit.CrossX2.Graphics.Font;

/// <summary>
/// IFont interface defines the properties and methods required for handling font details and text measurement.
/// </summary>
public interface IFont
{
    /// <summary>
    /// Gets the name of the font.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Gets the size of the font.
    /// </summary>
    int Size { get; }

    /// <summary>
    /// Gets the line size of the font, which represents the total height used by a line of text, including any extra spacing.
    /// </summary>
    int LineSize { get; }
    
    /// <summary>
    /// Calculates the size of the specified text.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <returns>A <see cref="Size"/> structure representing the width and height of the text.</returns>
    Size TextSize(TextSource text, TextSpacing spacing = TextSpacing.Normal);
}