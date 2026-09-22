using System.Reflection;
using Ssit.CrossX2.Framework.Games.Editor;

namespace Ssit.CrossX2.Editor.Models.Parameters;

public class ParameterBooleanModel: ParameterModel<bool>
{
    public ParameterBooleanModel(string name, object owner, PropertyInfo propertyInfo, IPropertyHandler handler) 
        : base(name, owner, propertyInfo, handler)
    {
    }
}