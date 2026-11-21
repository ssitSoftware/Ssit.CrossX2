using Ssit.CrossX2.Framework.Games.Services;
using Ssit.CrossX2.Framework.Services;

namespace Ssit.CrossX2.Framework.Games.Logic;

/// <summary>
/// Picks a fixed physics time delta (1 / TargetUps, UPS = physics updates per second) that stays in sync with the display.
/// TargetUps is always a whole multiple of the screen refresh rate (60 Hz → 60, 120, 180, 240...;
/// 50 Hz → 50, 100, 150...; 144 Hz → 144, 288...), so every rendered frame runs a whole number
/// of physics updates. Only when even the refresh rate itself is above MaxUps, a whole
/// divisor of it is used instead (240 Hz → 120, 80, 60...).
/// </summary>
internal class GameTimer(IAppWindowManager windowManager, IUpsInfoService upsInfoService, GameTimer.Parameters parameters): IGameTimer
{
    public class Parameters
    {
        public float? MinUps;
        public float? MaxUps;
    }

    private const float MeasureWindow = 1f;   // seconds of frames averaged for the fps measurement
    private const int MaxMultiplier = 16;     // hz * k, k = 1..MaxMultiplier
    private const int MaxDivisor = 8;         // hz / n, n = 2..MaxDivisor
    private const float FallbackUps = 60f;    // used when neither refresh rate nor fps is known yet

    private readonly List<float> _candidates = [];

    private float _measureTime;
    private int _framesCount;
    private float _measuredFps;
    private double _lastRefreshRate = -1;

    public float TargetUps { get; private set; } = 120f;
    public float TimeDelta { get; private set; } = 1 / 120f;

    public void Update(float deltaTime)
    {
        var refreshRate = windowManager.GetRefreshRate();
        var recalculate = false;

        // Display changed (window moved to another monitor, mode switch) or first frame - react at once.
        if (Math.Abs(refreshRate - _lastRefreshRate) > 0.01)
        {
            _lastRefreshRate = refreshRate;
            recalculate = true;
        }

        _measureTime += deltaTime;
        _framesCount++;

        if (_measureTime >= MeasureWindow)
        {
            _measuredFps = _framesCount / _measureTime;
            _measureTime = 0;
            _framesCount = 0;
            recalculate = true;
        }

        if (!recalculate)
            return;

        TargetUps = CalculateTargetUps((float)refreshRate, _measuredFps);
        upsInfoService.SetUps((int)MathF.Round(TargetUps));

        TimeDelta = 1f / TargetUps;
    }

    private float CalculateTargetUps(float refreshRate, float measuredFps)
    {
        var minUps = parameters.MinUps ?? 0f;
        var maxUps = parameters.MaxUps ?? float.MaxValue;

        if (minUps > maxUps)
            (minUps, maxUps) = (maxUps, minUps);

        // Base rate the simulation should be aligned to: the display, or the measured fps if the display is unknown.
        var baseUps = refreshRate > 0 ? refreshRate
            : measuredFps > 0 ? MathF.Round(measuredFps)
            : FallbackUps;

        // What we compare candidates against - how fast frames really come right now.
        var currentFps = measuredFps > 0 ? measuredFps : baseUps;

        _candidates.Clear();

        for (var k = 1; k <= MaxMultiplier; k++)
            _candidates.Add(baseUps * k);

        // Divisors only matter when even the base rate is too fast for MaxUps.
        if (baseUps > maxUps)
        {
            for (var n = 2; n <= MaxDivisor; n++)
                _candidates.Add(baseUps / n);
        }

        // 1) Prefer a candidate inside [min, max] that is closest to the current frame rate.
        //    Ties go to the lower (cheaper) one.
        var best = 0f;
        var bestDistance = float.MaxValue;

        foreach (var candidate in _candidates)
        {
            if (candidate < minUps || candidate > maxUps)
                continue;

            var distance = MathF.Abs(candidate - currentFps);
            if (distance < bestDistance || (distance == bestDistance && candidate < best))
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        if (best > 0)
            return best;

        // 2) No display-aligned rate fits the range (e.g. 144 Hz with range 100..120).
        //    Take the candidate nearest to the range and clamp it, so the delta never leaves [min, max].
        bestDistance = float.MaxValue;

        foreach (var candidate in _candidates)
        {
            var distance = candidate < minUps ? minUps - candidate : candidate - maxUps;
            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        return Math.Clamp(best, minUps, maxUps);
    }
}
