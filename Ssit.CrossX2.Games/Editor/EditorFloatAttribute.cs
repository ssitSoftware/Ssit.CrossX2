namespace Ssit.CrossX2.Framework.Games.Editor;

public class EditorFloatAttribute: EditorAttribute
{
    public float Min { get; }
    public float Max { get; }
    public float Step { get; }

    public EditorFloatAttribute(float min, float max, float step = 1, Type handlerType = null): base(handlerType)
    {
        Min = min;
        Max = max;
        Step = step;
    }
}