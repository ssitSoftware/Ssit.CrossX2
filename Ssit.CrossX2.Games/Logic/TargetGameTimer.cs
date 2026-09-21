using Ssit.CrossX2.Framework.Services;

namespace Ssit.CrossX2.Framework.Games.Logic;

public class TargetGameTimer : IGameTimer
{
    public class Parameters
    {
        public int TargetFps { get; set; }
    }
    
    public TargetGameTimer(Parameters parameters, IAppWindowManager appWindowManager)
    {
        var fps = appWindowManager.GetDisplayMode().hz;
        fps =  Math.Max(fps, 60);
        
        var min= parameters.TargetFps * 0.9f;
        var max = parameters.TargetFps * 1.81;
        
        while (fps < min)
        {
            fps *= 2;
        }
        while (fps > max)
        {
            fps /= 2;
        }
        
        TimeDelta = 1.0f / fps;
    }

    public void Update(float dt) { }
    public float TimeDelta { get; }
}