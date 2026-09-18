namespace Ssit.CrossX2.Services;

public interface IAppWindowManager
{
    bool IsFullscreen { get; }
    event Action<WindowClosingEventArgs> Closing; 
    void Close();
    bool SetFullscreen();
    bool SetWindowed(Size size);
    void SetTitle(string title);
    bool IsTouchScreen { get; }
    Size GetWindowMaxSize();
    
    (int w, int h, int hz) GetDisplayMode();
}