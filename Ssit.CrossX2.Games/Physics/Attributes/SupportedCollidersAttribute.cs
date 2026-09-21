namespace Ssit.CrossX2.Framework.Games.Physics.Attributes;

public class SupportedCollidersAttribute(params Type[] types) : Attribute
{
    public Type[] Types { get; } = types;
}