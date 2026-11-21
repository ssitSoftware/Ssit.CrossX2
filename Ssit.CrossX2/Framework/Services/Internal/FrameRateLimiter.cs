namespace Ssit.CrossX2.Framework.Services.Internal;

/// <summary>
/// Frame pacing for <see cref="VSyncMode.Limit"/>, independent of the backend's clock and sleep functions.
/// <para>
/// The target frame rate is the largest whole multiple of the display refresh rate that doesn't exceed
/// <see cref="MaxFps"/>, so every refresh shows the same number of rendered frames and the frame time is
/// constant (60 Hz → 480, 144 Hz → 432, 180 Hz → 360, 240 Hz → 480). When the multiple would be below
/// 2× the refresh rate the limit is off (<see cref="TargetFps"/> = 0).
/// </para>
/// </summary>
internal sealed class FrameRateLimiter(double maxFps = FrameRateLimiter.DefaultMaxFps)
{
    public const double DefaultMaxFps = 480;

    /// <summary>Smallest refresh rate multiple worth limiting to; below it frames are not limited.</summary>
    private const int MinRefreshRateMultiplier = 2;

    private ulong _nextFrameStartNs;

    public double MaxFps { get; } = maxFps;

    /// <summary>Frame rate frames are paced to; 0 when not limited.</summary>
    public double TargetFps { get; private set; }

    /// <summary>Frame time of <see cref="TargetFps"/> in nanoseconds; 0 when not limited.</summary>
    public ulong TargetFrameTimeNs { get; private set; }

    /// <summary>
    /// Exact refresh rate of the display in Hz (e.g. 59.94). 0 turns the limit off.
    /// Cheap to set every frame: the target is only recalculated when the value changes.
    /// </summary>
    public double RefreshRate
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            TargetFps = CalculateTargetFps(value, MaxFps);
            TargetFrameTimeNs = TargetFps > 0 ? (ulong)Math.Round(1e9 / TargetFps) : 0;
            Reset();
        }
    }

    /// <summary>
    /// Largest whole multiple of <paramref name="refreshRate"/> up to <paramref name="maxFps"/>; 0 when it is below 2×.
    /// The multiplier comes from the nominal (rounded) rate, so 59.94 Hz and 60.02 Hz both get 8× at 480 FPS,
    /// while the result keeps the exact rate so frames stay in step with the display.
    /// </summary>
    public static double CalculateTargetFps(double refreshRate, double maxFps)
    {
        if (!(refreshRate > 0))
            return 0;

        var nominalRate = Math.Max(1, Math.Round(refreshRate));
        var multiplier = (int)Math.Floor(maxFps / nominalRate);

        return multiplier >= MinRefreshRateMultiplier ? refreshRate * multiplier : 0;
    }

    /// <summary>Restarts the cadence; the next frame starts immediately.</summary>
    public void Reset() => _nextFrameStartNs = 0;

    /// <summary>
    /// Call once per frame, before it starts. Returns how long to wait (ns) before starting it.
    /// <para>
    /// Frame start times advance by exactly <see cref="TargetFrameTimeNs"/> from the previous deadline rather
    /// than from "now", so sleep overshoot and timer jitter don't accumulate and the average rate stays exactly
    /// on <see cref="TargetFps"/>: a slightly late frame is followed by a shorter wait. After a stall longer than
    /// a whole frame (loading, window drag, pause) the cadence restarts instead of rushing through missed frames.
    /// </para>
    /// </summary>
    public ulong GetWaitTimeNs(ulong nowNs)
    {
        var frameTimeNs = TargetFrameTimeNs;

        if (frameTimeNs == 0)
            return 0;

        if (_nextFrameStartNs == 0 || nowNs >= _nextFrameStartNs + frameTimeNs)
        {
            _nextFrameStartNs = nowNs + frameTimeNs;
            return 0;
        }

        var frameStartNs = _nextFrameStartNs;
        _nextFrameStartNs += frameTimeNs;

        return frameStartNs > nowNs ? frameStartNs - nowNs : 0;
    }
}
