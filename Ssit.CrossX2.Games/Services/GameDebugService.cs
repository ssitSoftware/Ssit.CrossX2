using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.Games.Services;

public class GameDebugService: IGameDebugService
{
    public SharedBool ShowDebug => _showDebug;

    private readonly SharedBoolMutable _showDebug = new(false);
    
    public void ToggleDebug()
    {
        _showDebug.SetValue(!_showDebug.Value);
    }
}