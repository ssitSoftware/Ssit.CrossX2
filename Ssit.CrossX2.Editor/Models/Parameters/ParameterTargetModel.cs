using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Ssit.CrossX2.Framework.Games.Editor;
using Ssit.CrossX2.Framework.Games.Map;

namespace Ssit.CrossX2.Editor.Models.Parameters;

public class ParameterTargetModel : ParameterModel<int>
{
    private readonly Type _targetType;
    private readonly Func<Type, Task<MapObject>> _findMatchingObject;
    private readonly MapFile _map;

    public ICommand SelectObjectCommand { get; }

    public string Info
    {
        get;
        private set => SetField(ref field, value);
    }

    public ParameterTargetModel(string name, object source, PropertyInfo propertyInfo, 
        Type targetType, Func<Type, Task<MapObject>> findMatchingObject,
        IPropertyHandler handler,
        MapFile map)
        : base(name, source, propertyInfo, handler)
    {
        _targetType = targetType;
        _findMatchingObject = findMatchingObject;
        _map = map;

        SelectObjectCommand = new AsyncRelayCommand(FindObject);
        UpdateInfo();
    }

    private async Task FindObject()
    {
        try
        {
            var obj = await _findMatchingObject.Invoke(_targetType);
            Value = obj?.Id ?? 0;
            UpdateInfo();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void UpdateInfo()
    {
        if (Value != 0)
        {
            var obj = _map.FindObject(Value);
            if (obj == null || obj.Type == null || !_targetType.IsAssignableFrom(obj.Type))
            {
                Value = 0;
                UpdateInfo();
                return;
            }
            Info = $"{obj.TypeId.Split('/').Last()} (#{Value})";
        }
        else
        {
            Info = "-";
        }
    }
}