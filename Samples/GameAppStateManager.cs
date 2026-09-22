using Ssit.CrossX2.Framework.Core;
using Ssit.CrossX2.Framework.Graphics;
using Ssit.CrossX2.Framework.Graphics.Effects;
using Ssit.CrossX2.Framework.Input;
using Ssit.CrossX2.Framework.IoC;
using Ssit.CrossX2.Framework.Services;

namespace Samples;

internal class GameAppStateManager: IDisposable, IIoCPostRegisterHandler, IUpdatable
{
    private readonly IAppWindowManager _windowManager;
    private readonly IRenderHostParameters _hostParameters;
    private readonly IKeyboard _keyboard;
    private readonly CrtSimulationEffectParameters _crtSimulationEffectParameters;
    private readonly IActionScheduler _actionScheduler;
    private readonly IRenderHost _renderHost;

    public GameAppStateManager(IAppWindowManager windowManager, IRenderHostParameters hostParameters, 
        IKeyboard keyboard, CrtSimulationEffectParameters crtSimulationEffectParameters,
        IActionScheduler actionScheduler,
        IRenderHost renderHost)
    {
        _windowManager = windowManager;
        _hostParameters = hostParameters;
        _keyboard = keyboard;
        _crtSimulationEffectParameters = crtSimulationEffectParameters;
        _actionScheduler = actionScheduler;
        _renderHost = renderHost;
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
        if (_keyboard.GetKey(Key.F9) == ButtonState.JustPressed)
        {
            if (_hostParameters.Flags.HasFlag(RenderHostFlags.EnableCrtSimulation))
            {
                _hostParameters.Flags &= ~RenderHostFlags.EnableCrtSimulation;
            }
            else
            {
                _hostParameters.Flags |= RenderHostFlags.EnableCrtSimulation;
            }
            _actionScheduler.Schedule(_renderHost.Apply);
        }

        if (_keyboard.GetKey(Key.Escape) == ButtonState.JustPressed)
        {
            _windowManager.Close();
        }

        if (_keyboard.GetKey(Key.D) == ButtonState.JustPressed)
        {
            _crtSimulationEffectParameters.BarrelDistortion = _crtSimulationEffectParameters.BarrelDistortion < 0.01f ? 0.025f : 0.0f; 
        }
        
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
            
            _actionScheduler.Schedule(_renderHost.Apply);
        }
    }
}

