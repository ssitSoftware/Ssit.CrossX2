using System.Text;

namespace Ssit.CrossX2.Text;

public readonly struct TextSource
{
    private readonly StringBuilder _builder;
    private readonly string _string;
    private readonly ICharProvider _provider;
    private readonly int _start;

    public int Length { get; }
    
    public TextSource(string str, int start = 0, int length = -1) 
    {
        _string = str;
        _builder = null;
        _provider = null;
        _start = start;
        Length = length >= 0  ? length : str.Length - start;
        Length = Math.Min(Length, str.Length - start);
    }
    
    public TextSource(StringBuilder str, int start = 0, int length = -1)
    {
        _string = null;
        _builder = str;
        _provider = null;
        _start = start;
        Length = length >= 0  ? length : str.Length - start;
        Length = Math.Min(Length, str.Length - start);
    }
    
    public TextSource(ICharProvider str, int start = 0, int length = -1)
    {
        _string = null;
        _builder = null;
        _provider = str;
        Length = length >= 0  ? length : str.Length - start;
        Length = Math.Min(Length, str.Length - start);
    }

    public TextSource(TextSource source, int start, int length = -1)
    {
        _string = source._string;
        _builder = source._builder;
        _provider = source._provider;

        _start = start + source._start;
        
        Length = length >= 0  ? length : source.Length - start;
        Length = Math.Min(Length, source.Length - start);
    }
    
    public char this[int index] => index >= Length ? '\0' :
        _provider is not null ? _provider[index + _start] : _builder is not null ? _builder[index + _start] : _string[index + _start];

    public override string ToString()
    {
        return _string is not null ? _string.Substring(_start, Length) :
            _builder is not null ? _builder.ToString(_start, Length) : _provider.ToString(_start, Length);
    }

    public override int GetHashCode() => _provider?.GetHashCode() ?? _builder?.GetHashCode() ?? _string.GetHashCode();

    public static implicit operator TextSource(string str) => new(str);
    public static implicit operator TextSource(StringBuilder str) => new(str);
    public static implicit operator TextSource(CharProvider str) => new(str);
}