using Ssit.CrossX2.Framework.Games.Physics;

namespace Ssit.CrossX2.Framework.Games.Logic;

public interface INpcCharacter
{
    void SetInRange(IBodyOwner attachedBodyOwner, bool inRange);
    bool CanStartConversation { get; }
    void PrepareCameraForTalking();
    Task StartConversation(float posX, string conversationId = null);
    float TalkingDistance { get; }
    IBody Body { get; }
}