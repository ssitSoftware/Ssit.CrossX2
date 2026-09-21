namespace Ssit.CrossX2.Framework.Games.Template;

public class ObjectDescription: ImageDescription
{
    public readonly int DefaultZOrder;
    public readonly Type TargetType;
    public readonly Type ParametersType;

    public ObjectDescription(string name, Type targetType, string sprite, string spriteSequence, Type parametersType = null, int defaultZOrder = 0): base(name, sprite, spriteSequence)
    {
        DefaultZOrder = defaultZOrder;
        TargetType = targetType;
        ParametersType = parametersType;
    }
}