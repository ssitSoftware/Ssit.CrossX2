using System;
using System.Collections.Generic;

namespace Ssit.CrossX2.Editor.Models;

public class StringTreeItem
{
    public string Name { get; }
    public string Value { get; }

    public IReadOnlyList<StringTreeItem> Children => (IReadOnlyList<StringTreeItem>)_children ?? Array.Empty<StringTreeItem>();

    private List<StringTreeItem> _children;

    public StringTreeItem(string name, string value)
    {
        Name = name;
        Value = value;
    }
    
    public void AddChild(StringTreeItem child)
    {
        if (_children is null)
        {
            _children = new List<StringTreeItem>();
        }
        _children.Add(child);
    }
}