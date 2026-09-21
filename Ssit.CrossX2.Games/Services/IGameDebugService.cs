using Ssit.CrossX2.Framework.UI.Values;

namespace Ssit.CrossX2.Framework.Games.Services;

public interface IGameDebugService
{
    SharedBool ShowDebug { get; }
    void ToggleDebug();
}