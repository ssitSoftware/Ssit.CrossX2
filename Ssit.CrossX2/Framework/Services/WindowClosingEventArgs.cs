namespace Ssit.CrossX2.Framework.Services;

/// <summary>
/// Event data for a window close request, allowing the close operation to be canceled.
/// </summary>
public class WindowClosingEventArgs : EventArgs
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public bool Cancel { get; set; }
}
