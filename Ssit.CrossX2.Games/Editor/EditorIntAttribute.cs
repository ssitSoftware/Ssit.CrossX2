namespace Ssit.CrossX2.Framework.Games.Editor;

public class EditorIntAttribute: EditorAttribute
{
    public int Min { get; }
    public int Max { get; }
    public int Step { get; }

    public EditorIntAttribute(int min, int max, int step = 1, Type validatorType = null, string conditionPropertyName = null): base(validatorType, conditionPropertyName)
    {
        Min = min;
        Max = max;
        Step = step;
    }
}