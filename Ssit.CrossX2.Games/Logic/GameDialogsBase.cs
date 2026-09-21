using Ssit.CrossX2.Framework.Services;

namespace Ssit.CrossX2.Framework.Games.Logic;

public abstract class GameDialogsBase(IActionScheduler actionScheduler) : IGameDialogs
{
    protected readonly IActionScheduler ActionScheduler = actionScheduler;
    private Action<int> _onReply;
    protected bool ShouldHide { get; set; }

    public void Show(string text, string[] replyOptions, Action<int> onReply = null)
    {
        ShouldHide = false;
        OnReply(-1);
        
        SetValuesForDialog(text, replyOptions);
        _onReply = onReply;
    }

    protected abstract void SetValuesForDialog(string text, string[] replyOptions);

    public Task<int> ShowAsync(string text, string[] replyOptions)
    {
        var tcs = new TaskCompletionSource<int>();
        
        ActionScheduler.Schedule( () => Show(text, replyOptions, index =>
        {
            if (!tcs.Task.IsCompleted)
            {
                tcs.SetResult(index);
            }

            _onReply = null;
        }));
        return tcs.Task;
    }

    protected void OnReply(int index)
    {
        _onReply?.Invoke(index);
        _onReply = null;
    }

    public abstract bool IsConversationActive { get; }
}