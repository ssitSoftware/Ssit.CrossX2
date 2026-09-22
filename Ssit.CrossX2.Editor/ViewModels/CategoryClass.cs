using System;
using Ssit.CrossX2.Framework.Utils;

namespace Ssit.CrossX2.Editor.ViewModels;

public class CategoryClass: BindableModel
{
    private readonly Action _onChanged;
    public string Name { get; }

    public bool IsSelected
    {
        get;
        set
        {
            if (SetField(ref field, value))
            {
                _onChanged?.Invoke();
            }
        }
    }

    public CategoryClass(string name, Action onChanged)
    {
        _onChanged = onChanged;
        Name = name;
    }
}