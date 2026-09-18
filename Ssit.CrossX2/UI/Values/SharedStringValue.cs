using System.Text;
using Ssit.CrossX2.Text;

namespace Ssit.CrossX2.UI.Values;

public class SharedStringValue : SharedString
{
    private readonly StringBuilder _builder = new();

    public override int Length => _builder.Length;
    public override char this[int index] => _builder[index];

    public SharedStringValue()
    {
    }
    
    public SharedStringValue(string value)
    {
        _builder.Append(value);
    }

    public void SetText(string text)
    {
        _builder.Clear();
        _builder.Append(text);
        RaiseTextChanged();
    }
    
    public void SetText(ICharProvider text)
    {
        _builder.Clear();
        for (var idx = 0; idx < text.Length; idx++)
        {
            _builder.Append(text[idx]);    
        }
        
        RaiseTextChanged();
    }

    public void FormatText(string format, params object[] args)
    {
        _builder.Clear();
        _builder.AppendFormat(format, args);
        RaiseTextChanged();
    }

    public void AppendText(string format, params object[] args)
    {
        _builder.AppendFormat(format, args);
        RaiseTextChanged();
    }
    
    public static implicit operator SharedStringValue(string value) => new(value);
}

public class SharedStringJoin : SharedString
{
    private readonly SharedString _str1;
    private readonly SharedString _str2;

    public override int Length
    {
        get
        {
            lock (this)
            {
                return _str1.Length + _str2.Length;
            }
        }
    }

    public override char this[int index]
    {
        get
        {
            lock (this)
            {
                if (index >= _str1.Length)
                {
                    index -= _str1.Length;
                    return _str2[index];
                }

                return _str1[index];
            }
        }
    }
    
    public SharedStringJoin(SharedString str1, SharedString str2)
    {
        _str1 = str1;
        _str2 = str2;

        _str1.TextChanged += OnTextChanged;
        _str2.TextChanged += OnTextChanged;
    }

    private void OnTextChanged()
    {
        RaiseTextChanged();
    }
}