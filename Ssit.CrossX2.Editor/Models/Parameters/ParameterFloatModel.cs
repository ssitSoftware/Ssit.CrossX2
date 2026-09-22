using System.Reflection;
using Ssit.CrossX2.Framework.Games.Editor;

namespace Ssit.CrossX2.Editor.Models.Parameters;

public class ParameterFloatModel : ParameterModel<float>
{
    public float Min { get; }
    public float Max { get; }
    public float Step { get; }
    
    public ParameterFloatModel(string name,
        float min, float max, float step,
        object owner, PropertyInfo propertyInfo, IPropertyHandler handler) : base(name, owner, propertyInfo, handler)
    {
        Min = min;
        Max = max;
        Step = step;
    }

    protected override void Validate()
    {
        IsInvalid = Value < Min || Value > Max;
    }
}