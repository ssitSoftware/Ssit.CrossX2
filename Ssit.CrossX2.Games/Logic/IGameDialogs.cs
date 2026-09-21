namespace Ssit.CrossX2.Framework.Games.Logic;

public interface IGameDialogs
{
    void Show(string text, string[] replyOptions, Action<int> onReply = null);
    Task<int> ShowAsync(string text, string[] replyOptions);
    bool IsConversationActive { get; }
}