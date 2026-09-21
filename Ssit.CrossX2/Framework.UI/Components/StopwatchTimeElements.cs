namespace Ssit.CrossX2.Framework.UI.Components;

[Flags]
public enum StopwatchTimeElements
{
    None = 0,
    Hours = 1,
    Minutes = 2,
    Seconds = 4,
    TenthSeconds = 8,
    Milliseconds = 16
}