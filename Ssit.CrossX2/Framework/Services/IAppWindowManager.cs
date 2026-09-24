namespace Ssit.CrossX2.Framework.Services;

public interface IAppWindowManager
{
    bool IsFullscreen { get; }
    event Action<WindowClosingEventArgs> Closing; 
    void Close();
    bool SetFullscreen();
    bool SetWindowed(Size size, bool keepSize = true);
    void SetTitle(string title);
    bool IsTouchScreen { get; }
    Size GetWindowMaxSize();
    void SetMinimumSize(Size size);
    
    (int w, int h, int hz) GetDisplayMode();
}