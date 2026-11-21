namespace Ssit.CrossX2.Framework.Services;

/// <summary>
/// How presented frames are paced (<see cref="IAppWindowManager.VSyncMode"/>).
/// </summary>
public enum VSyncMode
{
    /// <summary>Presentation waits for the display refresh: frame rate = refresh rate, no tearing.</summary>
    VSync,

    /// <summary>
    /// VSync off, frames paced to the largest whole multiple of the display refresh rate up to 480 FPS
    /// (60 Hz → 480, 144 Hz → 432, 165 Hz → 330, 180 Hz → 360, 240 Hz → 480), so every refresh shows the
    /// same number of frames. Uncapped when no multiple of at least 2× fits (displays above 240 Hz).
    /// </summary>
    Limit,

    /// <summary>VSync off, frame rate uncapped.</summary>
    Full,
}
