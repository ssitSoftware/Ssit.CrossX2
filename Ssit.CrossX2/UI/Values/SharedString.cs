using Ssit.CrossX2.Text;

namespace Ssit.CrossX2.UI.Values;

public abstract class SharedString: CharProvider
{
    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((SharedString)obj);
    }

    private int _hashCode = -1;
    
    public event Action TextChanged;
    
    public static implicit operator SharedString(string value) => new SharedStringValue(value);
    public static SharedString operator + (SharedString str1, SharedString str2) => new SharedStringJoin(str1, str2);

    public static bool operator == (SharedString obj, string str)
    {
        if (obj is null) return false;
        if (str is null) return false;
        
        if (str.Length != obj.Length)
            return false;

        for (var idx = 0; idx < str.Length; idx++)
        {
            if(str[idx] != obj[idx])
                return false;
        }

        return true;
    }

    public static bool operator !=(SharedString obj, string str)
    {
        return !(obj == str);
    }

    protected void RaiseTextChanged()
    {
        TextChanged?.Invoke();
        _hashCode = -1;
    }

    public override int GetHashCode()
    {
        if (_hashCode == -1)
        {
            CalculateHashCode();
        }
        return _hashCode;
    }

    protected void CalculateHashCode()
    {
        const int prime = 37;
        int result = 1;

        for (var idx = 0; idx < Length; ++idx)
        {
            result = result * prime + this[idx].GetHashCode();
        }
    }
}