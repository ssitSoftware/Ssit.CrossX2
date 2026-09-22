using System.Reflection;
using Ssit.CrossX2.Framework.Games.Editor;

namespace Ssit.CrossX2.Editor.Models.Parameters;

public class ParameterIntModel : ParameterModel<int>
{
    public int Min { get; }
    public int Max { get; }
    public int Step { get; }

    public ParameterIntModel(string name,
        int min, int max, int step,
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