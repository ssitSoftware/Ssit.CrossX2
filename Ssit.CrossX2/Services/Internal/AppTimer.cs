using Ssit.CrossX2.Core;

namespace Ssit.CrossX2.Services.Internal;

internal class AppTimer: IAppTimer, IUpdatable
{
    public float RunTime { get; private set; } = 0;
    public void Update(float dt) => RunTime += dt;
}