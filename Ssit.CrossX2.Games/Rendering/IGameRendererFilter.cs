namespace Ssit.CrossX2.Framework.Games.Rendering;

public interface IGameRendererFilter
{
    int FilterMode { get; set; }

    /// <summary>
    /// True when an element with the given filter mode is rendered under the current <see cref="FilterMode"/>
    /// (filter off, or the element belongs to the active filter).
    /// </summary>
    bool IsVisible(int filterMode) => FilterMode == 0 || FilterMode == filterMode;
}