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
    bool SetWindowed();
    void SetWindowParameters(Size size, Size minimumSize, WindowedMode mode = WindowedMode.KeepAspect);
    void SetTitle(string title);
    bool IsTouchScreen { get; }
    Size GetWindowMaxSize();
    
    (int w, int h, int hz) GetDisplayMode();
}