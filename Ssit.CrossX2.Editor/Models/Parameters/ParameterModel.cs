using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Ssit.CrossX2.Framework.Games.Editor;
using Ssit.CrossX2.Framework.Utils;

namespace Ssit.CrossX2.Editor.Models.Parameters;

public abstract class ParameterModel : BindableModel
{
    public string Name { get; }

    protected ParameterModel(string name)
    {
        Name = name;
    }
}

public abstract class ParameterModel<TValue> : ParameterModel
{
    private TValue _value;
    private readonly object _owner;
    private readonly PropertyInfo _propertyInfo;
    private readonly IPropertyHandler _handler;

    public bool Enabled { get; }

    public TValue Value
    {
        get => _value;
        set
        {
            if (SetField(ref _value, value))
            {
                if (ValidateInternal())
                {
                    if (!_disableUpdatingEvent)
                    {
                        _handler?.OnUpdating();
                        _disableUpdatingEvent = true;
                    }

                    SetProperty(_value);
                    _handler?.OnUpdated();
                }
            }
        }
    }

    public bool IsInvalid
    {
        get;
        protected set => SetField(ref field, value);
    }

    public ICommand FocusLostCommand { get; }

    private bool _disableUpdatingEvent;

    protected ParameterModel(string name, object owner, PropertyInfo propertyInfo, IPropertyHandler handler) :
        base(name)
    {
        _owner = owner;
        _propertyInfo = propertyInfo;
        _handler = handler;

        var value = _propertyInfo.GetValue(owner);

        if (value is TValue val)
        {
            _value = val;
        }

        Enabled = true;
        if (false == _handler?.Enable(value))
        {
            Enabled = false;
        }

        FocusLostCommand = new RelayCommand(() => _disableUpdatingEvent = false);
    }

    protected virtual void Validate()
    {
        IsInvalid = false;
    }

    private void SetProperty(object value)
    {
        _propertyInfo.SetValue(_owner, value);
    }

    private bool ValidateInternal()
    {
        if (_handler != null)
        {
            IsInvalid = !_handler.Validate(Value);
        }
        else
        {
            Validate();
        }

        return !IsInvalid;
    }
}