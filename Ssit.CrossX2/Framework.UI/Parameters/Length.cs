using System.Globalization;

namespace Ssit.CrossX2.Framework.UI.Parameters;

public struct Length
{
    public static readonly Length Auto = new(float.NaN);
    public static readonly Length Fill = new(0,1);
    public static readonly Length Zero = new(0);
    
    public readonly float Value;
    public readonly float Percent;
    public readonly float Pixels;
    
    public bool IsAuto => float.IsNaN(Value);

    internal Length(float value = 0, float percent = 0, float pixels = 0)
    {
        Value = value;
        Percent = percent;
        Pixels = pixels;
    }

    private static string[] Split(string str)
    {
        var list = new List<string>();
        var startIndex = 0;
        for(var idx =0; idx < str.Length; ++idx)
        {
            if (str[idx] == '+' || str[idx] == '-')
            {
                list.Add(str.Substring(startIndex, idx - startIndex));
                list.Add(str[idx].ToString());
                startIndex = idx + 1;
            }
        }
        list.Add(str.Substring(startIndex, str.Length - startIndex));
        return list.ToArray();
    }
    
    private Length(string str)
    {
        if (str.ToLowerInvariant() == "auto")
        {
            Value = float.NaN;
            return;
        }
        
        if (str.ToLowerInvariant() == "zero")
        {
            return;
        }
        
        if (str.ToLowerInvariant() == "fill")
        {
            Percent = 1;
            return;
        }
        
        var parts = Split(str);

        float value = 0;
        float percent = 0;
        float pixels = 0;

        float sign = 1;

        foreach (var part in parts)
        {
            switch (part)
            {
                case "+":
                    sign = 1;
                    break;
                
                case "-":
                    sign = -1;
                    break;
                
                case "C":
                    percent += sign * 0.5f;
                    break;
                
                case "@":
                    percent += sign;
                    break;

                default:
                {
                        if (part.EndsWith('%'))
                        {
                            percent += sign * float.Parse(part.Substring(0, part.Length - 1), NumberStyles.Float,
                                CultureInfo.InvariantCulture) / 100f;
                        }
                        else if (part.EndsWith("px"))
                        {
                            pixels += sign * float.Parse(part.Substring(0, part.Length - 2), NumberStyles.Float,
                                CultureInfo.InvariantCulture) / 100f;
                        }
                        else
                        {
                            value += sign * float.Parse(part, NumberStyles.Float, CultureInfo.InvariantCulture);
                        }
                }
                break;
            }
        }
        
        Value = value;
        Percent = percent;
    }
    
    public float Calculate(float scale, float reference)
    {
        return IsAuto ? 0 : Value * scale + Percent * reference + Pixels;
    }
    
    public static implicit operator Length(float value) => new(value);
    public static implicit operator Length(string str) => new Length(str);

    public static Length operator +(Length length, Length addend) =>
        new Length(length.Value + addend.Value, length.Percent + addend.Percent, length.Pixels + addend.Pixels);
    
    public static Length operator -(Length length, Length sub) =>
        new Length(length.Value - sub.Value, length.Percent - sub.Percent, length.Pixels - sub.Pixels);
    
    public static Length operator *(Length length, float mul) =>
        new Length(length.Value * mul, length.Percent * mul, length.Pixels * mul);
}
