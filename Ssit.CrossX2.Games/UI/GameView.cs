using Ssit.CrossX2.Framework.UI.Values;
using Ssit.CrossX2.Framework.UI.Views;

namespace Ssit.CrossX2.Framework.Games.UI;

public class GameView: Background
{
    public IGameInstance GameInstance { get; set; }
    public SharedBool Active { get; set; }
    public SharedBool ShowDebug { get; set; }
    public float SpeedFactor { get; set; } = 1f;
}