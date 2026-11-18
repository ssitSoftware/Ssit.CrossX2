using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ssit.CrossX2.Editor.Helpers;
using Ssit.CrossX2.Framework.Games.Editor;

namespace Ssit.CrossX2.Editor.Models.Parameters;

public sealed class EnumValueItem
{
    public object Value { get; }
    public string DisplayName { get; }

    public EnumValueItem(object value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }

    public override string ToString() => DisplayName;
}

public class ParameterEnumModel : ParameterModel<object>
{
    public IReadOnlyList<EnumValueItem> Items { get; }

    public EnumValueItem SelectedItem
    {
        get => Items.FirstOrDefault(i => Equals(i.Value, Value));
        set
        {
            if (value is null) return;
            Value = value.Value;
            OnPropertyChanged();
        }
    }

    public ParameterEnumModel(string name, Type enumType, object owner, PropertyInfo propertyInfo, IPropertyHandler handler)
        : base(name, owner, propertyInfo, handler)
    {
        Items = Enum.GetValues(enumType)
            .Cast<object>()
            .Distinct()
            .Select(v => new EnumValueItem(v, Enum.GetName(enumType, v).ToDisplayName()))
            .ToList();
    }
}
