namespace Ssit.CrossX2.Editor;

public class EditorIntAttribute: EditorAttribute
{
    public int Min { get; }
    public int Max { get; }
    public int Step { get; }

    public EditorIntAttribute(int min, int max, int step = 1, Type validatorType = null): base(validatorType)
    {
        Min = min;
        Max = max;
        Step = step;
    }
}