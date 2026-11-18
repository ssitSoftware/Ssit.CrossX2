namespace Ssit.CrossX2.Framework.Services;

/// <summary>
/// Window sizing behavior used when the application runs in windowed mode.
/// </summary>
public enum WindowedMode
{
    None = 0,
    KeepAspect = 1,
    KeepSize = 2,
}

/// <summary>
/// Controls the application window state, size, title and close lifecycle.
/// </summary>
public interface IAppWindowManager
{
    bool IsFullscreen { get; }

    /// <summary>
    /// Synchronizes presentation with the display refresh rate (default true). When false the frame rate is uncapped.
    /// Ignored while the CROSSX_PRESENT_MODE environment variable forces a present mode.
    /// </summary>
    bool VSync { get; set; }

    event Action<WindowClosingEventArgs> Closing; 
    void Close();
    bool SetFullscreen();
    bool SetWindowed();
    void SetWindowParameters(Size size, Size minimumSize, WindowedMode mode = WindowedMode.KeepAspect);
    void SetTitle(string title);
    bool IsTouchScreen { get; }
    Size GetWindowMaxSize();
    
    (int w, int h, int hz) GetDisplayMode();
}
