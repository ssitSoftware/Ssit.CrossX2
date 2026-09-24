namespace Ssit.CrossX2.Framework.Services;

public enum WindowedMode
{
    None = 0,
    KeepAspect = 1,
    KeepSize = 2,
}

public interface IAppWindowManager
{
    bool IsFullscreen { get; }
    event Action<WindowClosingEventArgs> Closing; 
    void Close();
    bool SetFullscreen();
    bool SetWindowed(Size size, WindowedMode mode = WindowedMode.KeepAspect);
    void SetTitle(string title);
    bool IsTouchScreen { get; }
    Size GetWindowMaxSize();
    void SetMinimumSize(Size size);
    
    (int w, int h, int hz) GetDisplayMode();
}