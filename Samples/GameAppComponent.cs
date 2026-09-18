using Ssit.CrossX2.Core;

namespace Samples;

internal class GameAppComponent(IAppComponent component) : WrapperAppComponent(component)
{
}