using Ssit.CrossX2.Framework.Games.Template;

namespace Ssit.CrossX2.Framework.Games.Editor;

public class EditorAttribute : Attribute
{
    public Type HandlerType { get; }
    public string ConditionPropertyName { get; }
    
    public EditorAttribute(string conditionPropertyName = null)
    {
        ConditionPropertyName = conditionPropertyName;
    }

    public EditorAttribute(Type handlerType, string conditionPropertyName = null)
    {
        HandlerType = handlerType;
        ConditionPropertyName = conditionPropertyName;
    }
}