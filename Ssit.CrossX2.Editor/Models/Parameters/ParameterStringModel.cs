using System.Reflection;
using Ssit.CrossX2.Framework.Games.Editor;

namespace Ssit.CrossX2.Editor.Models.Parameters;

public class ParameterStringModel : ParameterModel<string>
{
    public ParameterStringModel(string name, object owner, PropertyInfo propertyInfo, IPropertyHandler handler) : base(name, owner, propertyInfo, handler)
    {
    }

    protected override void Validate()
    {
        IsInvalid = string.IsNullOrWhiteSpace(Value);
    }
}