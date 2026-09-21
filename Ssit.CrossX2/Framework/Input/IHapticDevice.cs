using System.Numerics;

namespace Ssit.CrossX2.Framework.Input;

public interface IHapticDevice: IDisposable
{
    FeedbackLevel UiFeedbackLevel { get; set; }
    FeedbackLevel ForceFeedbackLevel { get; set; }

    void Feedback(FeedbackStyle style, Vector2? position = null);
}