using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Input;
using Ssit.CrossX2.Framework.IoC;
using Ssit.CrossX2.Framework.Services;

namespace Samples;

internal class GameAppStateManager: IDisposable, IIoCPostRegisterHandler, IUpdatable
{
    private readonly IAppWindowManager _windowManager;
    private readonly IRenderHostParameters _hostParameters;
    private readonly IKeyboard _keyboard;

    public GameAppStateManager(IAppWindowManager windowManager, IRenderHostParameters hostParameters, IKeyboard keyboard)
    {
        _windowManager = windowManager;
        _hostParameters = hostParameters;
        _keyboard = keyboard;
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }

    public void OnAllServicesRegistered()
    {
        _windowManager.SetWindowed(_hostParameters.DesignSize * 2);
    }
    
    void IUpdatable.Update(float dt)
    {
        if (_keyboard.GetKey(Key.F11) == ButtonState.JustPressed)
        {
            if (_windowManager.IsFullscreen)
            {
                _windowManager.SetWindowed(_hostParameters.DesignSize * 2);
            }
            else
            {
                _windowManager.SetFullscreen();
            }
        }
    }
}

