namespace Ssit.CrossX2.Framework.Games.Logic.Narration;

public interface INarrationSystem
{
    /// <summary>
    /// Raised on the game thread when an option with an <c>Action</c> is chosen. The dialog waits for the handlers:
    /// when one sets <see cref="NarrationActionRequest.Result"/> to false, the dialog stays at the same text.
    /// </summary>
    event Action<NarrationActionRequest> NarrationAction;
    Task StartNarration(string subject);
    bool HasRequest(string subject);
    bool HasNarration(string subject);
    INarrationSystem SetValue(string key, string value);
}