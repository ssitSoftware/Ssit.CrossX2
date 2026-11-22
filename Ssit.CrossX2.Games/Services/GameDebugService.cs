using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.Games.Services;

internal class GameDebugService: IGameDebugService, IUpsInfoService
{
    public SharedBool ShowDebug => _showDebug;

    private readonly SharedBoolMutable _showDebug = new(false);
    public void ToggleDebug() => _showDebug.SetValue(!_showDebug.Value);

    public int Ups { get; private set; }
    public void SetUps(int ups) => Ups = ups;
}