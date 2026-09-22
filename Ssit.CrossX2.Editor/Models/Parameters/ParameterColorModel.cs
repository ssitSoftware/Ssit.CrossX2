using System.Reflection;
using Ssit.CrossX2.Framework;
using Ssit.CrossX2.Framework.Games.Editor;

namespace Ssit.CrossX2.Editor.Models.Parameters;

public class ParameterColorModel: ParameterModel<RgbaColor>
{
    public ParameterColorModel(string name, object owner, PropertyInfo propertyInfo, IPropertyHandler handler) : base(name, owner, propertyInfo, handler)
    {
    }
}