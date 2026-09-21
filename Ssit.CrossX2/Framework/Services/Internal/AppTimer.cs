using Ssit.CrossX2.Framework.Core;

namespace Ssit.CrossX2.Framework.Services.Internal;

internal class AppTimer: IAppTimer, IUpdatable
{
    public float RunTime { get; private set; } = 0;
    public void Update(float dt) => RunTime += dt;
}