namespace Ssit.CrossX2.Framework.Games.Logic.Objects;

public interface IStoryOperator
{
    bool ExecuteStoryConversation(INpcCharacter npc, string conversationId = null);
}