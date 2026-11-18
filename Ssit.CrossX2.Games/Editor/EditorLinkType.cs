namespace Ssit.CrossX2.Framework.Games.Editor;

/// <summary>
/// How the editor displays a link (<see cref="EditorLinkAttribute"/>) between an object and the linked object.
/// </summary>
public enum EditorLinkType
{
    /// <summary>
    /// Arrow from the object to the linked object.
    /// </summary>
    Point,
    
    /// <summary>
    /// Circle around the object with the radius equal to the distance to the linked object (e.g. a range).
    /// </summary>
    Circular,
    
    /// <summary>
    /// Rectangle with the top left corner at the object and the bottom right corner at the linked object (e.g. an area).
    /// </summary>
    Area
}
