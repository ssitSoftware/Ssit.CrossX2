using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Ssit.CrossX2.Framework.Xml;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public readonly struct XNodeAttributes
{
    private readonly XNode _node;
    public XNodeAttributes(XNode node)
    {
        this._node = node;
    }

    public bool HasAttribute(string name)
    {
        return _node.HasAttribute(name);
    }

    public int AsInt32(string name, int defaultValue)
    {
        return int.TryParse(_node.Attribute(name), out var value) ? value : defaultValue;
    }

    public double AsDouble(string name, double defaultValue)
    {
        return double.TryParse(_node.Attribute(name), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : defaultValue;
    }
    
    public double AsPercentOrScalar(string name, double fullValue, double defaultValue)
    {
        var val = _node.Attribute(name);
        if (val is null)
        {
            return defaultValue;
        }

        if (val.EndsWith('%'))
        {
            val = val[..^1];
            
            int.TryParse(val, out var outlineInt);
            return outlineInt * fullValue / 100f;
        }

        double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out var result);
        return result;
    }

    public decimal AsDecimal(string name, decimal defaultValue)
    {
        return decimal.TryParse(_node.Attribute(name), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : defaultValue;
    }

    public string AsString(string name, string defaultValue = "")
    {
        return HasAttribute(name) ? _node.Attribute(name) : defaultValue;
    }
    public Guid AsGuid(string name)
    {
        var guidStr = _node.Attribute(name) ?? "";

        return !Guid.TryParse(guidStr, out var guid) ? Guid.NewGuid() : guid;
    }

    public bool AsBoolean(string name, bool defaultValue = false)
    {
        return bool.TryParse(_node.Attribute(name), out var value) ? value : defaultValue;
    }

    public bool Parse2Int(string name, out int v1, out int v2)
    {
        v1 = 0;
        v2 = 0;

        var text = _node.Attribute(name);

        if (text == null) return false;

        var parts = text.Split(',');
        if (parts.Length != 2) return false;

        return int.TryParse(parts[0].Trim(), out v1) && int.TryParse(parts[1].Trim(), out v2);
    }

    public bool Parse4Int(string name, out int v1, out int v2, out int v3, out int v4)
    {
        v1 = 0;
        v2 = 0;
        v3 = 0;
        v4 = 0;

        var text = _node.Attribute(name);

        if (text == null) return false;

        var parts = text.Split(',');
        if (parts.Length != 4) return false;

        return int.TryParse(parts[0].Trim(), out v1) &&
               int.TryParse(parts[1].Trim(), out v2) && 
               int.TryParse(parts[2].Trim(), out v3) &&
               int.TryParse(parts[3].Trim(), out v4);
    }

    public bool Parse2Double(string name, out double v1, out double v2)
    {
        v1 = 0;
        v2 = 0;

        var text = _node.Attribute(name);

        if (text == null) return false;

        var parts = text.Split(',');
        if (parts.Length != 2) return false;

        return double.TryParse(parts[0].Trim(), out v1) && double.TryParse(parts[1].Trim(), out v2);
    }

    public T AsEnum<T>(string name, T defaultValue) where T : struct, IConvertible
    {
        var attr = _node.Attribute(name)?.Replace("-", "")?.Replace(" ", "");
        if (attr == null) return defaultValue;

        return !Enum.TryParse<T>(attr, true, out var value) ? defaultValue : value;
    }
}