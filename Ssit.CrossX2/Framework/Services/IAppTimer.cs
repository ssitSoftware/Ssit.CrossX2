namespace Ssit.CrossX2.Framework.Services;

/// <summary>
/// Tracks elapsed application runtime in seconds.
/// </summary>
public interface IAppTimer
{
    float RunTime { get; }
}
