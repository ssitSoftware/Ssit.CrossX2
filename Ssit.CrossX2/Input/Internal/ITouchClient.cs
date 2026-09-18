namespace Ssit.CrossX2.Input.Internal;

public interface ITouchClient
{
    bool OnDown(ITouchEntity entity);
    bool OnMove(ITouchEntity entity);
    void OnUp(ITouchEntity entity);
    void OnCancel(int id, object capturedBy = null);
    void Reset();
}