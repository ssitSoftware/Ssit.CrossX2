namespace Ssit.CrossX2.Graphics.Font;

public enum ScaleMode
{
    None,
    Integer,
    Float
}

/// <summary>
/// Service managing fonts in the application.
/// </summary>
public interface IFontsManager
{
    /// <summary>
    /// Sets the default font to be used by the font manager.
    /// </summary>
    /// <param name="name">The name of the font to set as the default.</param>
    void SetDefaultFont(string name);
    
    /// <summary>
    /// Loads font definitions from a JSON stream and initializes the fonts' collection.
    /// </summary>
    /// <param name="fontsJsonPath">A path to the JSON data describing the fonts.</param>
    void LoadFonts(string fontsJsonPath);

    /// <summary>
    /// Loads a bitmap font using the specified name, image file path, and size.
    /// </summary>
    /// <param name="name">The name to assign to the bitmap font.</param>
    /// <param name="imagePath">The path to the image file containing the font's bitmap representation.</param>
    /// <param name="size">The dimensions of the font's single character.</param>
    void LoadBitmapFont(string name, string imagePath, Size size);
    
    /// <summary>
    /// Retrieves a font with the specified name and size from the font manager.
    /// </summary>
    /// <param name="name">The name of the font to retrieve.</param>
    /// <param name="size">The size of the font to retrieve.</param>
    /// <param name="scaleMode">Mode for scaling</param>
    /// <returns>Pair of an <see cref="IFont"/> instance corresponding to the specified name and size, or null if not found and scaling factor.</returns>
    IFont GetFont(string name, float size = 0);
}