using Ssit.CrossX2.Graphics;
using Ssit.CrossX2.Services;

namespace Samples;

internal class GameAppStateManager: IDisposable
{
    private readonly IAppWindowManager _windowManager;
    private readonly IRenderHostParameters _hostParameters;

    public GameAppStateManager(IAppWindowManager windowManager, IRenderHostParameters hostParameters)
    {
        _windowManager = windowManager;
        _hostParameters = hostParameters;

        _windowManager.SetWindowed(_hostParameters.DesignSize * 2);
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}